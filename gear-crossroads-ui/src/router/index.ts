import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import Setups from '../views/SetupsView.vue'
import SetupDetail from '../views/SetupDetailView.vue'
import ItemDetailView from '../views/ItemDetailView.vue'
import ErrorView from '../views/ErrorView.vue'
import ConfirmEmailView from '../views/ConfirmEmailView.vue'
import ResetPasswordView from '../views/ResetPasswordView.vue'
import TermsOfServiceView from '../views/TermsOfServiceView.vue'
import ContactUsView from '../views/ContactUsView.vue'
import AdminAccountsView from '../views/AdminAccountsView.vue'
import AdminAccountDetailView from '../views/AdminAccountDetailView.vue'

const routes = [
    { path: '/', name: 'Home', component: HomeView },
    { path: '/login', name: 'Login', component: LoginView },
    { path: '/register', name: 'Register', component: RegisterView },
    { path: '/confirm-email', name: 'confirm-email', component: ConfirmEmailView },
    { path: '/request-password-reset', name: 'request-password-reset', component: () => import('../views/RequestPasswordResetView.vue') },
    { path: '/reset-password', name: 'reset-password', component: ResetPasswordView },
    { path: '/terms-of-service', name: 'TermsOfService', component: TermsOfServiceView },
    { path: '/contact', name: 'Contact', component: ContactUsView },
    {
        path: '/feed',
        name: 'feed',
        component: () => import('../views/FeedView.vue')
    },
    {
        path: '/upvoted',
        name: 'upvoted',
        component: () => import('../views/UpvotedView.vue'),
        meta: { requiresAuth: true }
    },
    { path: '/setups', name: 'Setups', component: Setups, meta: { requiresAuth: true } },
    { path: '/setups/:id', name: 'SetupDetail', component: SetupDetail, props: true },
    { path: '/items/:id', name: 'ItemDetail', component: ItemDetailView, props: true },
    { path: '/admin/accounts', name: 'AdminAccounts', component: AdminAccountsView, meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/admin/accounts/:id', name: 'AdminAccountDetail', component: AdminAccountDetailView, meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/error', name: 'error', component: ErrorView },

]

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
})

router.beforeEach(async (to, _from, next) => {
    const { useUserStore } = await import('../stores/user')
    const { useAlertStore } = await import('../stores/alert')
    const store = useUserStore()
    const alertStore = useAlertStore()

    // Check if token is expired on every navigation
    if (store.token && store.isTokenExpired()) {
        await store.logout()

        // Show alert message if user was trying to access a protected route
        if (to.meta.requiresAuth) {
            alertStore.show('Your session has expired. Please log in again.', 'error', 4000)
            next('/login')
            return
        }
    }

    if (to.meta.requiresAuth && !store.token) {
        next('/login')
    } else if (to.meta.requiresAdmin && !store.isAdmin) {
        next('/')
    } else {
        next()
    }
})

export default router
