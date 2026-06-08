import { createApp } from 'vue';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import App from './App.vue';
import 'primeicons/primeicons.css';
import './styles.css';

const app = createApp(App);
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1
    }
  }
});

app.use(VueQueryPlugin, { queryClient });

app.mount('#app');
