<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { toast } from 'vue-sonner';
import { getKycDocumentFile, getKycDocumentFileUrl } from '../api';
import { formatDate } from '../utils/formatters';
import { cardBase, labelBase, primaryButton, secondaryButton } from './ui';

const props = defineProps({
  documents: {
    type: Array,
    default: () => []
  },
  isLoading: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['upload']);

const documentTypes = [
  'National ID',
  'Proof of address',
  'Passport',
  'Business registration',
  'Income proof'
];

const maxFiles = 5;
const allowedContentTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
const selectedDocuments = ref([]);
const fileInput = ref(null);
const pendingUpdateDocumentType = ref('');
const previewDocument = ref(null);
const previewDocumentUrl = ref('');
const previewError = ref('');
const isPreviewLoading = ref(false);
let activePreviewObjectUrl = '';

const selectedDocumentTypes = computed(() => selectedDocuments.value.map((document) => document.documentType));
const documentsSignature = computed(() => {
  return props.documents
    .map((document) => `${document.id}:${document.documentType}:${document.originalFileName}:${document.uploadedAtUtc}`)
    .join('|');
});
const hasTooManyFiles = computed(() => selectedDocuments.value.length > maxFiles);
const hasDuplicateTypes = computed(() => new Set(selectedDocumentTypes.value).size !== selectedDocumentTypes.value.length);
const previewContentType = computed(() => previewDocument.value?.contentType?.toLowerCase() ?? '');
const canPreviewInline = computed(() => previewContentType.value === 'application/pdf' || previewContentType.value.startsWith('image/'));
const canSubmit = computed(() => selectedDocuments.value.length > 0
  && !hasTooManyFiles.value
  && !hasDuplicateTypes.value
  && selectedDocuments.value.every((document) => document.documentType && document.file));

function formatFileSize(size) {
  if (size >= 1_000_000) {
    return `${(size / 1_000_000).toFixed(1)} MB`;
  }

  return `${Math.max(1, Math.round(size / 1000))} KB`;
}

function getExistingDocument(documentType) {
  return props.documents.find((document) => document.documentType === documentType);
}

function isAllowedFile(file) {
  return allowedContentTypes.includes(file.type);
}

function getNextDocumentType(index) {
  return documentTypes[index] ?? documentTypes[0];
}

function handleFileChange(event) {
  let files = Array.from(event.target.files ?? []);

  if (files.length === 0) {
    return;
  }

  if (pendingUpdateDocumentType.value && files.length > 1) {
    files = files.slice(0, 1);
    toast.error(`Only one replacement file is needed for ${pendingUpdateDocumentType.value}. The first file was selected.`);
  }

  const unsupportedFile = files.find((file) => !isAllowedFile(file));
  if (unsupportedFile) {
    toast.error(`${unsupportedFile.name} is not supported. Upload PDF, JPG, PNG, or WEBP documents only.`);
    event.target.value = '';
    return;
  }

  const oversizedFile = files.find((file) => file.size > 5_000_000);
  if (oversizedFile) {
    toast.error(`${oversizedFile.name} is too large. Each document must be 5MB or less.`);
    event.target.value = '';
    return;
  }

  selectedDocuments.value = files.map((file, index) => ({
    id: `${file.name}-${file.size}-${file.lastModified}-${index}`,
    file,
    documentType: pendingUpdateDocumentType.value || getNextDocumentType(index)
  }));

  pendingUpdateDocumentType.value = '';

  if (selectedDocuments.value.length > maxFiles) {
    toast.error('You selected more than five documents. Remove extra files before uploading.');
  }
}

function removeSelectedDocument(documentId) {
  selectedDocuments.value = selectedDocuments.value.filter((document) => document.id !== documentId);

  if (selectedDocuments.value.length === 0 && fileInput.value) {
    fileInput.value.value = '';
  }
}

function clearSelectedDocuments() {
  selectedDocuments.value = [];
  pendingUpdateDocumentType.value = '';
  if (fileInput.value) {
    fileInput.value.value = '';
  }
}

function chooseReplacementFile(documentType) {
  pendingUpdateDocumentType.value = documentType;
  fileInput.value?.click();
}

function revokePreviewObjectUrl() {
  if (activePreviewObjectUrl) {
    URL.revokeObjectURL(activePreviewObjectUrl);
    activePreviewObjectUrl = '';
  }

  previewDocumentUrl.value = '';
}

async function previewUploadedDocument(document) {
  revokePreviewObjectUrl();
  previewDocument.value = document;
  previewError.value = '';
  isPreviewLoading.value = true;

  try {
    const blob = await getKycDocumentFile(document.id);

    if (previewDocument.value?.id !== document.id) {
      return;
    }

    const previewBlob = blob.type
      ? blob
      : new Blob([blob], { type: document.contentType || 'application/octet-stream' });
    activePreviewObjectUrl = URL.createObjectURL(previewBlob);
    previewDocumentUrl.value = activePreviewObjectUrl;
  } catch (error) {
    previewError.value = error.message;
    toast.error(error.message);
  } finally {
    if (previewDocument.value?.id === document.id) {
      isPreviewLoading.value = false;
    }
  }
}

function closeDocumentPreview() {
  previewDocument.value = null;
  previewError.value = '';
  isPreviewLoading.value = false;
  revokePreviewObjectUrl();
}

function downloadUploadedDocument(documentId) {
  window.open(getKycDocumentFileUrl(documentId, { download: true }), '_blank', 'noopener,noreferrer');
}

function submit() {
  if (selectedDocuments.value.length === 0) {
    toast.error('Choose at least one PDF or image document to upload.');
    return;
  }

  if (hasTooManyFiles.value) {
    toast.error('Only five KYC documents are expected. Remove extra files before uploading.');
    return;
  }

  if (hasDuplicateTypes.value) {
    toast.error('Each selected file must have a different document type.');
    return;
  }

  emit('upload', {
    documents: selectedDocuments.value.map((document) => ({
      documentType: document.documentType,
      file: document.file
    }))
  });
}

watch(documentsSignature, () => {
  if (selectedDocuments.value.length > 0) {
    clearSelectedDocuments();
  }
});

onBeforeUnmount(() => {
  revokePreviewObjectUrl();
});
</script>

<template>
  <section class="grid items-start gap-5 xl:grid-cols-[420px_minmax(0,1fr)]">
    <form :class="[cardBase, 'grid content-start gap-4 p-4']" @submit.prevent="submit">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">KYC</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Upload documents</h2>
      </div>

      <label
        class="grid cursor-pointer place-items-center rounded-2xl border-2 border-dashed border-emerald-200 bg-emerald-50/60 px-4 py-8 text-center transition hover:border-emerald-400 hover:bg-emerald-50"
      >
        <input
          ref="fileInput"
          class="sr-only"
          type="file"
          multiple
          accept="application/pdf,image/jpeg,image/jpg,image/png,image/webp"
          @change="handleFileChange"
        />
        <span class="grid size-12 place-items-center rounded-2xl bg-white text-emerald-700 shadow-sm">
          <i class="pi pi-cloud-upload text-xl"></i>
        </span>
        <span class="mt-3 text-sm font-black text-slate-950">Choose PDF or image documents</span>
        <span class="mt-1 text-xs font-semibold text-slate-500">Select up to five files. Each file can be National ID, Proof of address, Passport, Business registration, or Income proof.</span>
      </label>

      <div v-if="selectedDocuments.length" class="grid gap-3">
        <div class="flex items-center justify-between gap-3">
          <p class="text-xs font-black uppercase tracking-wide text-slate-500">Selected files</p>
          <button type="button" :class="secondaryButton" @click="clearSelectedDocuments">
            <i class="pi pi-times"></i>
            Clear
          </button>
        </div>

        <p v-if="hasTooManyFiles" class="rounded-xl bg-red-50 px-3 py-2 text-xs font-bold text-red-700">
          More than five documents selected. Remove extra files before uploading.
        </p>
        <p v-if="hasDuplicateTypes" class="rounded-xl bg-amber-50 px-3 py-2 text-xs font-bold text-amber-700">
          Select a different document type for each file.
        </p>

        <article
          v-for="document in selectedDocuments"
          :key="document.id"
          class="grid gap-3 rounded-2xl border border-slate-200 bg-white p-3"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <p class="break-all text-sm font-black text-slate-950">{{ document.file.name }}</p>
              <p class="mt-1 text-xs font-semibold text-slate-500">{{ document.file.type }} · {{ formatFileSize(document.file.size) }}</p>
            </div>
            <button
              type="button"
              class="grid size-9 shrink-0 place-items-center rounded-xl bg-slate-100 text-slate-500 transition hover:bg-red-50 hover:text-red-600"
              aria-label="Remove selected document"
              @click="removeSelectedDocument(document.id)"
            >
              <i class="pi pi-trash"></i>
            </button>
          </div>

          <label :class="labelBase">
            This document is
            <select v-model="document.documentType" class="h-11 rounded-xl border border-slate-200 bg-slate-50 px-3 text-sm font-bold text-slate-900 outline-none transition focus:border-emerald-500 focus:bg-white focus:ring-4 focus:ring-emerald-500/10">
              <option v-for="type in documentTypes" :key="type">{{ type }}</option>
            </select>
          </label>

          <p v-if="getExistingDocument(document.documentType)" class="text-xs font-semibold text-amber-700">
            Uploading this will update your existing {{ document.documentType }} document.
          </p>
        </article>
      </div>

      <button type="submit" :class="primaryButton" :disabled="isLoading || !canSubmit">
        <i class="pi pi-upload"></i>
        {{ isLoading ? 'Uploading...' : 'Upload selected documents' }}
      </button>
    </form>

    <section :class="[cardBase, 'grid gap-4 p-4']">
      <div>
        <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Submitted files</p>
        <h2 class="text-lg font-black tracking-tight text-slate-950">Document history</h2>
      </div>

      <div v-if="documents.length === 0" class="grid min-h-36 place-items-center rounded-2xl border border-dashed border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500">
        No KYC documents uploaded yet.
      </div>

      <div v-else class="grid gap-2">
        <article v-for="document in documents" :key="document.id" class="rounded-2xl border border-slate-200 bg-slate-50 p-3">
          <div class="flex items-start justify-between gap-3">
            <div>
              <strong class="text-sm text-slate-950">{{ document.documentType }}</strong>
              <p class="break-all text-xs font-semibold text-slate-500">{{ document.originalFileName }}</p>
            </div>
            <div class="flex shrink-0 flex-wrap items-center justify-end gap-2">
              <span class="rounded-xl bg-white px-2 py-1 text-[10px] font-black uppercase text-slate-500">
                {{ document.contentType }}
              </span>
              <button type="button" :class="secondaryButton" @click="previewUploadedDocument(document)">
                <i class="pi pi-eye"></i>
                Preview
              </button>
              <button type="button" :class="secondaryButton" @click="chooseReplacementFile(document.documentType)">
                <i class="pi pi-upload"></i>
                Update
              </button>
              <button type="button" :class="secondaryButton" @click="downloadUploadedDocument(document.id)">
                <i class="pi pi-download"></i>
                Download
              </button>
            </div>
          </div>
          <p class="mt-2 text-xs font-semibold text-slate-500">{{ formatDate(document.uploadedAtUtc) }}</p>
        </article>
      </div>
    </section>

    <Teleport to="body">
      <div
        v-if="previewDocument"
        class="fixed inset-0 z-[55] grid place-items-center bg-slate-950/55 p-4 backdrop-blur-sm"
        role="dialog"
        aria-modal="true"
      >
        <section class="grid max-h-[92vh] w-full max-w-5xl grid-rows-[auto_minmax(0,1fr)_auto] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
          <header class="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
            <div class="min-w-0">
              <p class="text-[11px] font-extrabold uppercase tracking-wide text-emerald-700">Document preview</p>
              <h2 class="truncate text-xl font-black tracking-tight text-slate-950">{{ previewDocument.documentType }}</h2>
              <p class="mt-1 break-all text-xs font-semibold text-slate-500">{{ previewDocument.originalFileName }}</p>
            </div>
            <button
              type="button"
              class="grid size-9 place-items-center rounded-xl bg-slate-100 text-slate-500 transition hover:bg-slate-200 hover:text-slate-950"
              aria-label="Close document preview"
              @click="closeDocumentPreview"
            >
              <i class="pi pi-times"></i>
            </button>
          </header>

          <div class="min-h-0 bg-slate-100 p-3">
            <div
              v-if="isPreviewLoading"
              class="grid h-full min-h-[70vh] place-items-center rounded-xl border border-slate-200 bg-white p-6 text-center"
            >
              <div>
                <i class="pi pi-spin pi-spinner text-3xl text-emerald-600"></i>
                <p class="mt-3 text-sm font-bold text-slate-700">Loading document preview</p>
              </div>
            </div>
            <div
              v-else-if="previewError"
              class="grid h-full min-h-[70vh] place-items-center rounded-xl border border-slate-200 bg-white p-6 text-center"
            >
              <div class="max-w-md">
                <i class="pi pi-exclamation-triangle text-3xl text-amber-500"></i>
                <p class="mt-3 text-sm font-bold text-slate-800">{{ previewError }}</p>
                <p class="mt-2 text-xs font-semibold text-slate-500">Use download if your browser cannot render this file inline.</p>
              </div>
            </div>
            <iframe
              v-else-if="previewDocumentUrl && previewContentType === 'application/pdf'"
              class="h-full min-h-[70vh] w-full rounded-xl border border-slate-200 bg-white"
              :src="previewDocumentUrl"
              title="KYC document preview"
            ></iframe>
            <div v-else-if="previewDocumentUrl && previewContentType.startsWith('image/')" class="grid h-full min-h-[70vh] place-items-center overflow-auto rounded-xl border border-slate-200 bg-white p-4">
              <img
                class="max-h-full max-w-full object-contain"
                :src="previewDocumentUrl"
                alt="KYC document preview"
              />
            </div>
            <div
              v-else-if="previewDocumentUrl && !canPreviewInline"
              class="grid h-full min-h-[70vh] place-items-center rounded-xl border border-slate-200 bg-white p-6 text-center"
            >
              <div class="max-w-md">
                <i class="pi pi-file text-3xl text-slate-500"></i>
                <p class="mt-3 text-sm font-bold text-slate-800">This file type cannot be previewed inline.</p>
                <p class="mt-2 text-xs font-semibold text-slate-500">Download the document to view it on your device.</p>
              </div>
            </div>
          </div>

          <footer class="flex justify-end gap-2 border-t border-slate-100 p-4">
            <button type="button" :class="secondaryButton" @click="closeDocumentPreview">Close</button>
            <button type="button" :class="primaryButton" @click="downloadUploadedDocument(previewDocument.id)">
              <i class="pi pi-download"></i>
              Download document
            </button>
          </footer>
        </section>
      </div>
    </Teleport>
  </section>
</template>
