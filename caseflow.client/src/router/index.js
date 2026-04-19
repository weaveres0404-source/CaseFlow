import { createRouter, createWebHistory } from 'vue-router'

const Login = () => import('../components/Login.vue')
const MainLayout = () => import('../layouts/MainLayout.vue')
const Dashboard = () => import('../views/Dashboard.vue')
const CaseList = () => import('../views/CaseList.vue')
const CaseCreate = () => import('../views/CaseCreate.vue')
const CaseDetail = () => import('../views/CaseDetail.vue')
const Notifications = () => import('../views/Notifications.vue')
const Profile = () => import('../views/Profile.vue')
const ReportsHours = () => import('../views/ReportsHours.vue')
const ReportsCases = () => import('../views/ReportsCases.vue')
const ProblemCategories = () => import('../views/ProblemCategories.vue')

const routes = [
  { path: '/login', name: 'Login', component: Login },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: 'dashboard' },
      { path: 'dashboard', name: 'Dashboard', component: Dashboard },
      { path: 'cases', name: 'CaseList', component: CaseList },
      { path: 'cases/new', name: 'CaseCreate', component: CaseCreate },
      { path: 'cases/:id', name: 'CaseDetail', component: CaseDetail, props: true },
      { path: 'notifications', name: 'Notifications', component: Notifications },
      { path: 'profile', name: 'Profile', component: Profile },
      { path: 'reports/hours', name: 'ReportsHours', component: ReportsHours },
      { path: 'reports/cases', name: 'ReportsCases', component: ReportsCases },
      { path: 'problem-categories', name: 'ProblemCategories', component: ProblemCategories }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('access_token')
  if (to.matched.some(r => r.meta.requiresAuth) && !token) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.name === 'Login' && token) {
    next({ name: 'Dashboard' })
  } else {
    next()
  }
})

export default router
