<template>
  <div class="p-6">
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-semibold">問題分類管理</h1>
      <button @click="openCreate" class="px-4 py-2 bg-indigo-600 text-white rounded">新增分類</button>
    </div>

    <div class="bg-white shadow rounded">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">名稱</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">描述</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">排序</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">狀態</th>
            <th class="px-6 py-3"></th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="cat in categories" :key="cat.id">
            <td class="px-6 py-4 whitespace-nowrap">{{ cat.name }}</td>
            <td class="px-6 py-4 whitespace-nowrap">{{ cat.description }}</td>
            <td class="px-6 py-4 whitespace-nowrap">{{ cat.sort_order }}</td>
            <td class="px-6 py-4 whitespace-nowrap">{{ cat.is_active ? '啟用' : '停用' }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="openEdit(cat)" class="text-indigo-600 hover:text-indigo-900 mr-3">編輯</button>
              <button @click="confirmDelete(cat)" class="text-red-600 hover:text-red-900">刪除</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed z-10 inset-0 overflow-y-auto">
      <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 transition-opacity" aria-hidden="true">
          <div class="absolute inset-0 bg-gray-500 opacity-75"></div>
        </div>

        <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">​</span>

        <div class="inline-block align-bottom bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
          <div>
            <h3 class="text-lg leading-6 font-medium text-gray-900 mb-4">{{ isEdit ? '編輯分類' : '新增分類' }}</h3>

            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700">名稱</label>
                <input v-model="form.categoryName" type="text" class="mt-1 block w-full border rounded px-3 py-2" />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">描述</label>
                <input v-model="form.description" type="text" class="mt-1 block w-full border rounded px-3 py-2" />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700">排序</label>
                <input v-model.number="form.sortOrder" type="number" class="mt-1 block w-full border rounded px-3 py-2" />
              </div>

              <div class="flex items-center">
                <input id="active" type="checkbox" v-model="form.isActive" class="mr-2" />
                <label for="active" class="text-sm text-gray-700">啟用</label>
              </div>

              <div v-if="formError" class="text-red-600">{{ formError }}</div>

              <div class="mt-4 flex justify-end">
                <button @click="closeModal" class="mr-3 px-4 py-2 border rounded">取消</button>
                <button @click="submitForm" class="px-4 py-2 bg-indigo-600 text-white rounded">{{ isEdit ? '更新' : '新增' }}</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script>
import axios from 'axios'
import { ref, onMounted } from 'vue'

export default {
  name: 'ProblemCategories',
  setup() {
    const categories = ref([])
    const page = ref(1)
    const pageSize = ref(20)
    const total = ref(0)

    const showModal = ref(false)
    const isEdit = ref(false)
    const form = ref({ categoryName: '', description: '', sortOrder: 10, isActive: true, id: null })
    const formError = ref('')

    async function load() {
      try {
        const res = await axios.get(`/api/v1/problem-categories?page=${page.value}&page_size=${pageSize.value}`)
        if (res.data && res.data.success) {
          categories.value = res.data.data
          total.value = res.data.meta?.total || 0
        }
      } catch (e) {
        console.error(e)
      }
    }

    function openCreate() {
      isEdit.value = false
      form.value = { categoryName: '', description: '', sortOrder: 10, isActive: true, id: null }
      formError.value = ''
      showModal.value = true
    }

    function openEdit(cat) {
      isEdit.value = true
      form.value = { categoryName: cat.name, description: cat.description, sortOrder: cat.sort_order || 10, isActive: cat.is_active, id: cat.id }
      formError.value = ''
      showModal.value = true
    }

    function closeModal() {
      showModal.value = false
    }

    async function submitForm() {
      formError.value = ''
      if (!form.value.categoryName) {
        formError.value = '名稱為必填'
        return
      }

      try {
        if (isEdit.value && form.value.id) {
          const res = await axios.put(`/api/v1/problem-categories/${form.value.id}`, {
            categoryName: form.value.categoryName,
            description: form.value.description,
            sortOrder: form.value.sortOrder,
            isActive: form.value.isActive
          })
          if (res.data && res.data.success) {
            closeModal()
            await load()
          }
        } else {
          const res = await axios.post('/api/v1/problem-categories', {
            categoryName: form.value.categoryName,
            description: form.value.description,
            sortOrder: form.value.sortOrder,
            isActive: form.value.isActive
          })
          if (res.data && res.data.success) {
            closeModal()
            await load()
          }
        }
      } catch (err) {
        console.error(err)
        formError.value = '儲存失敗'
      }
    }

    async function confirmDelete(cat) {
      if (!confirm(`確定刪除：${cat.name} ?`)) return
      try {
        const res = await axios.delete(`/api/v1/problem-categories/${cat.id}`)
        if (res.data && res.data.success) {
          await load()
        }
      } catch (e) {
        console.error(e)
        alert('刪除失敗')
      }
    }

    onMounted(() => {
      load()
    })

    return { categories, page, pageSize, total, showModal, isEdit, form, formError, openCreate, openEdit, closeModal, submitForm, confirmDelete }
  }
}
</script>

<style scoped>
/* minor adjustments for table */
</style>
