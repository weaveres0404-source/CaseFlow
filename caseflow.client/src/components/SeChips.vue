<template>
  <span v-if="members.length === 0" class="text-slate-300">—</span>

  <span
    v-else-if="members.length === 1"
    class="inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1"
    :class="memberRoleClass(members[0]?.role)">
    {{ members[0].name }}
  </span>

  <span v-else class="inline-flex items-center gap-1 group/se relative min-w-0 max-w-full">
    <span
      class="inline-flex min-w-0 items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1"
      :class="memberRoleClass(members[0]?.role)">
      <span class="truncate">{{ members[0].name }}</span>
    </span>
    <span class="tabular-nums text-[10px] px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600 ring-1 ring-slate-200 group-hover/se:bg-brand-50 group-hover/se:text-brand-700 group-hover/se:ring-brand-200 transition cursor-help">
      +{{ members.length - 1 }}
    </span>
    <span class="absolute left-0 top-full mt-1.5 hidden group-hover/se:block z-20 min-w-[180px] max-w-[280px] bg-slate-900 text-slate-100 text-[11px] rounded-lg shadow-lg p-2.5">
      <span class="block text-[10px] text-slate-400 mb-1">共 {{ members.length }} 位專案成員</span>
      <span v-for="(member, index) in members" :key="`${member.id ?? member.name}-${index}`" class="py-0.5 flex items-center gap-1.5">
        <span class="w-1.5 h-1.5 rounded-full" :class="memberRoleDotClass(member.role)"></span>
        <span class="break-words [overflow-wrap:anywhere]">{{ member.name }}</span>
      </span>
      <span class="absolute -top-1 left-4 w-2 h-2 bg-slate-900 rotate-45"></span>
    </span>
  </span>
</template>

<script setup>
import { computed } from 'vue'

defineOptions({
  name: 'SeChips'
})

const props = defineProps({
  ses: {
    type: Array,
    default: () => []
  }
})

const members = computed(() => props.ses
  .map((item) => {
    if (typeof item === 'string') {
      const name = item.trim()
      return name ? { id: null, name, role: null } : null
    }

    const name = item?.full_name?.trim() || item?.name?.trim() || ''
    if (!name) return null

    return {
      id: item?.id ?? null,
      name,
      role: item?.role || null
    }
  })
  .filter(Boolean))

function memberRoleClass(role) {
  const roleClassMap = {
    SysAdmin: 'bg-violet-50 text-violet-700 ring-violet-200',
    PM: 'bg-amber-50 text-amber-700 ring-amber-200',
    SE: 'bg-sky-50 text-sky-700 ring-sky-200'
  }
  return roleClassMap[role] || 'bg-slate-100 text-slate-700 ring-slate-200'
}

function memberRoleDotClass(role) {
  const roleDotMap = {
    SysAdmin: 'bg-violet-400',
    PM: 'bg-amber-400',
    SE: 'bg-sky-400'
  }
  return roleDotMap[role] || 'bg-slate-400'
}
</script>