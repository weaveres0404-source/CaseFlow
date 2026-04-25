<template>
  <span v-if="names.length === 0" class="text-slate-300">—</span>

  <span v-else-if="names.length === 1" class="text-slate-700 text-[13px]">{{ names[0] }}</span>

  <span v-else class="inline-flex items-center gap-1 group/se relative">
    <span class="text-slate-700 text-[13px]">{{ names[0] }}</span>
    <span class="tabular-nums text-[10px] px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-600 ring-1 ring-slate-200 group-hover/se:bg-brand-50 group-hover/se:text-brand-700 group-hover/se:ring-brand-200 transition cursor-help">
      +{{ names.length - 1 }}
    </span>
    <span class="absolute left-0 top-full mt-1.5 hidden group-hover/se:block z-20 min-w-[160px] bg-slate-900 text-white text-[11px] rounded-lg shadow-lg p-2.5">
      <span class="block text-[10px] text-slate-400 mb-1">共 {{ names.length }} 位 SE</span>
      <span v-for="(name, index) in names" :key="`${name}-${index}`" class="py-0.5 flex items-center gap-1.5">
        <span class="w-1 h-1 rounded-full bg-slate-500"></span>
        <span>{{ name }}</span>
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

const names = computed(() => props.ses
  .map((item) => {
    if (typeof item === 'string') return item.trim()
    return item?.full_name?.trim() || item?.name?.trim() || ''
  })
  .filter(Boolean))
</script>