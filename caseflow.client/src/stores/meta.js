import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../utils/api'

export const useMetaStore = defineStore('meta', () => {
  const customers = ref([])
  const projects = ref([])
  const categories = ref([])
  const modules = ref([])
  const users = ref([])
  const projectMembers = ref([])
  const projectCodes = ref([])
  const caseTypes = ref([])
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
        projectCodes.value = res.data.project_codes || []
        caseTypes.value = res.data.case_types || []
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

  // 取出專案所有成員（PM + SE），excludeUserId 為排除的使用者（通常是當前登入者）
  function getProjectAllMembers(projectId, excludeUserId = null) {
    const memberUserIds = projectMembers.value
      .filter(pm => pm.project_id === projectId)
      .map(pm => pm.user_id)
    return users.value.filter(u => memberUserIds.includes(u.id) && u.id !== excludeUserId)
  }

  // 依專案取出可用案件類型；無 project_ids 限制者視為共用
  function getCaseTypesForProject(projectId) {
    if (!projectId) return caseTypes.value
    return caseTypes.value.filter(t => !t.project_ids?.length || t.project_ids.includes(projectId))
  }

  // 依案件類型取出問題分類；分類無 case_type_ids 限制者視為共用
  function getCategoriesForCaseType(caseTypeId, projectId = null) {
    return categories.value.filter(c => {
      const matchType = !c.case_type_ids?.length || c.case_type_ids.includes(caseTypeId)
      const matchProj = !projectId || !c.project_ids?.length || c.project_ids.includes(projectId)
      return matchType && matchProj
    })
  }

  const statusMap = {
    10: { label: '待處理', color: 'bg-gray-100 text-gray-800' },
    20: { label: '已派工', color: 'bg-blue-100 text-blue-800' },
    30: { label: '處理中', color: 'bg-yellow-100 text-yellow-800' },
    35: { label: '已退回', color: 'bg-red-100 text-red-800' },
    40: { label: '已完工', color: 'bg-green-100 text-green-800' },
    50: { label: '已結案', color: 'bg-violet-100 text-violet-700' },
    60: { label: '已取消', color: 'bg-gray-200 text-gray-500' }
  }

  const priorityMap = {
    HIGH: { label: '高', color: 'bg-red-100 text-red-800' },
    MEDIUM: { label: '中', color: 'bg-yellow-100 text-yellow-800' },
    LOW: { label: '低', color: 'bg-green-100 text-green-800' }
  }

  // 動態以 caseTypes 表組成；fallback 顏色需與本機標籤色票一致。
  const CT_FALLBACK_COLORS = [
    'bg-red-100 text-red-800',
    'bg-purple-100 text-purple-800',
    'bg-blue-100 text-blue-800',
    'bg-teal-100 text-teal-800',
    'bg-sky-100 text-sky-800',
    'bg-amber-100 text-amber-800',
    'bg-rose-100 text-rose-800'
  ]
  const caseTypeMap = computed(() => {
    const map = {}
    caseTypes.value.forEach((t, i) => {
      map[t.code] = { label: t.label, color: t.color || CT_FALLBACK_COLORS[i % CT_FALLBACK_COLORS.length] }
    })
    return map
  })

  return {
    customers, projects, categories, modules, users, projectMembers, projectCodes, caseTypes, enums, loaded,
    fetchDropdowns, getModulesByProject, getUsersByRole, getProjectPMs, getProjectSEs, getProjectAllMembers,
    getCaseTypesForProject, getCategoriesForCaseType,
    statusMap, priorityMap, caseTypeMap
  }
})
