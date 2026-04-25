import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../utils/api'

export const useMetaStore = defineStore('meta', () => {
  const customers = ref([])
  const projects = ref([])
  const categories = ref([])
  const modules = ref([])
  const users = ref([])
  const projectMembers = ref([])
  const enums = ref({})
  const loaded = ref(false)

  async function fetchDropdowns() {
    try {
      const { data: res } = await api.get('/meta/dropdowns')
      if (res.success) {
        customers.value = res.data.customers
        projects.value = res.data.projects
        categories.value = res.data.categories
        modules.value = res.data.modules
        users.value = res.data.users
        projectMembers.value = res.data.project_members
        enums.value = res.data.enums
        loaded.value = true
      }
    } catch { /* 401 由 api 攔截器處理，其餘靜默 */ }
  }

  function getModulesByProject(projectId) {
    return modules.value.filter(m => m.project_id === projectId)
  }

  function getUsersByRole(role) {
    return users.value.filter(u => u.role === role)
  }

  function getProjectPMs(projectId) {
    const pmUserIds = projectMembers.value
      .filter(pm => pm.project_id === projectId && pm.role === 'PM')
      .map(pm => pm.user_id)
    return users.value.filter(u => pmUserIds.includes(u.id))
  }

  function getProjectSEs(projectId) {
    const seUserIds = projectMembers.value
      .filter(pm => pm.project_id === projectId && pm.role === 'SE')
      .map(pm => pm.user_id)
    return users.value.filter(u => seUserIds.includes(u.id))
  }

  const statusMap = {
    10: { label: '待處理', color: 'bg-slate-100 text-slate-600' },
    20: { label: '已派工', color: 'bg-blue-100 text-blue-700' },
    30: { label: '處理中', color: 'bg-amber-100 text-amber-700' },
    35: { label: '已退回', color: 'bg-red-100 text-red-700' },
    40: { label: '已完工', color: 'bg-green-100 text-green-700' },
    50: { label: '已結案', color: 'bg-violet-100 text-violet-700' },
    60: { label: '已取消', color: 'bg-rose-100 text-rose-600' }
  }

  const priorityMap = {
    HIGH: { label: '高', color: 'bg-red-100 text-red-800' },
    MEDIUM: { label: '中', color: 'bg-yellow-100 text-yellow-800' },
    LOW: { label: '低', color: 'bg-green-100 text-green-800' }
  }

  const caseTypeMap = {
    REPAIR: { label: '叫修', color: 'bg-orange-100 text-orange-800' },
    INQUIRY: { label: '功能諮詢', color: 'bg-blue-100 text-blue-800' },
    EVALUATION: { label: '評估需求', color: 'bg-purple-100 text-purple-800' }
  }

  return {
    customers, projects, categories, modules, users, projectMembers, enums, loaded,
    fetchDropdowns, getModulesByProject, getUsersByRole, getProjectPMs, getProjectSEs,
    statusMap, priorityMap, caseTypeMap
  }
})
