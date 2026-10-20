import { createRouter, createWebHistory } from 'vue-router'

const Login = () => import('../components/Login.vue')
const SetupPassword = () => import('../views/SetupPassword.vue')
const MainLayout = () => import('../layouts/MainLayout.vue')
const Dashboard = () => import('../views/Dashboard.vue')
const CaseList = () => import('../views/CaseList.vue')
const CaseCreate = () => import('../views/CaseCreate.vue')
const CaseDetail = () => import('../views/CaseDetail.vue')
const Notifications = () => import('../views/Notifications.vue')
const Profile = () => import('../views/Profile.vue')
const ReportsHours = () => import('../views/ReportsHours.vue')
const ProblemCategories = () => import('../views/ProblemCategories.vue')

const routes = [
  { path: '/login', name: 'Login', component: Login },
  { path: '/setup-password', name: 'SetupPassword', component: SetupPassword },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: 'dashboard' },
      { path: 'dashboard', name: 'Dashboard', component: Dashboard, meta: { customerRestricted: true } },
      { path: 'cases', name: 'CaseList', component: CaseList },
      { path: 'cases/new', name: 'CaseCreate', component: CaseCreate, meta: { customerRestricted: true } },
      { path: 'cases/:slug', name: 'CaseDetail', component: CaseDetail, props: true },
      { path: 'notifications', name: 'Notifications', component: Notifications, meta: { customerRestricted: true } },
      { path: 'profile', name: 'Profile', component: Profile },
      { path: 'reports/hours', name: 'ReportsHours', component: ReportsHours, meta: { customerRestricted: true } },
      { path: 'reports/cases', redirect: '/reports/hours', meta: { customerRestricted: true } },
      { path: 'problem-categories', name: 'ProblemCategories', component: ProblemCategories, meta: { customerRestricted: true } }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('access_token')
  let role
  try {
    role = JSON.parse(localStorage.getItem('user') || 'null')?.role || ''
  } catch {
    role = ''
  }

  if (to.matched.some(r => r.meta.requiresAuth) && !token) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (token && role === 'Customer' && to.matched.some(r => r.meta.customerRestricted)) {
    next({ name: 'CaseList' })
  } else if (to.name === 'Login' && token) {
    next({ name: role === 'Customer' ? 'CaseList' : 'Dashboard' })
  } else {
    next()
  }
})

export default router
