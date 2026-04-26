<template>
  <div class="px-1">
    <!-- Header -->
    <div class="flex items-start justify-between gap-3 mb-4">
      <div class="min-w-0">
        <div class="text-xs text-slate-500 mb-1 flex items-center gap-1">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="w-3 h-3 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">案件列表</span>
        </div>
        <div class="flex items-center gap-3">
          <h1 class="text-xl md:text-[22px] font-bold text-slate-900 tracking-tight">案件列表</h1>
          <span class="text-[11px] px-2 py-0.5 rounded-full bg-slate-100 text-slate-600 ring-1 ring-slate-200">{{ roleBadgeText }}</span>
        </div>
        <p class="text-sm text-slate-500 mt-0.5">目前以 <span class="font-medium text-slate-700">{{ authRoleLabel }}</span> 檢視：{{ scopeHint }}</p>
      </div>
      <div class="flex items-center gap-2 shrink-0">
        <button @click="exportCases" class="h-9 px-3 inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm text-slate-700">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/></svg>
          <span class="hidden sm:inline">匯出</span>
        </button>
        <button type="button" disabled class="h-9 px-3 inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white text-sm text-slate-400 cursor-not-allowed" title="欄位設定預留中">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M7 12h10M10 18h4"/></svg>
        </button>
        <router-link v-if="auth.role !== 'SE'" to="/cases/new"
          class="h-9 px-3.5 inline-flex items-center gap-1.5 rounded-lg bg-indigo-700 hover:bg-indigo-800 text-white text-sm font-medium shadow-sm">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
          新增案件
        </router-link>
      </div>
    </div>

    <!-- Quick Tabs -->
    <div class="flex items-center gap-1 mb-3 overflow-x-auto pb-1 thin-scroll">
      <button v-for="tab in quickTabs" :key="tab.key" @click="switchTab(tab.key)"
        class="h-8 px-3 rounded-lg text-xs font-medium whitespace-nowrap transition-all"
        :class="activeTab === tab.key
          ? 'bg-white border border-slate-300 text-slate-900 shadow-sm'
          : 'text-slate-600 hover:bg-white border border-transparent hover:border-slate-300'">
        {{ tab.label }}
        <span v-if="activeTab === tab.key && totalCount > 0" class="ml-1 tabular-nums text-slate-500">{{ totalCount }}</span>
      </button>
      <div class="flex-1 min-w-[8px]"></div>
      <button type="button" class="h-8 px-2 rounded-lg text-xs text-slate-500 hover:bg-white inline-flex items-center gap-1 whitespace-nowrap" @click="saveCurrentView">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5v14l7-3 7 3V5a2 2 0 00-2-2H7a2 2 0 00-2 2z"/></svg>
        儲存檢視
      </button>
    </div>

    <!-- Filter Bar -->
    <section class="bg-white border border-slate-200 rounded-xl mb-4 shadow-sm">
      <div class="p-2.5 flex items-center gap-2">
        <div class="relative flex-1">
          <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z"/>
          </svg>
          <input v-model="filters.q" type="text" placeholder="搜尋案件編號 / 報修人 / 問題描述..."
            class="w-full h-9 pl-9 pr-3 rounded-lg border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
            @keyup.enter="fetchCases(1)" />
        </div>
        <button @click="filterOpen = !filterOpen"
          class="h-9 px-3 inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-sm text-slate-700">
          <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="21" y1="4" x2="9" y2="4"/><line x1="3" y1="4" x2="5" y2="4"/><circle cx="7" cy="4" r="2"/>
            <line x1="21" y1="12" x2="13" y2="12"/><line x1="3" y1="12" x2="9" y2="12"/><circle cx="11" cy="12" r="2"/>
            <line x1="21" y1="20" x2="17" y2="20"/><line x1="3" y1="20" x2="15" y2="20"/><circle cx="15" cy="20" r="2"/>
          </svg>
          進階
          <span v-if="activeFilterCount > 0" class="ml-0.5 tabular-nums px-1.5 rounded bg-indigo-100 text-indigo-700 text-[11px]">{{ activeFilterCount }}</span>
          <svg class="w-4 h-4 transition-transform" :class="filterOpen ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        </button>
        <button @click="fetchCases(1)" class="h-9 px-4 rounded-lg bg-indigo-700 hover:bg-indigo-800 text-white text-sm font-medium">搜尋</button>
      </div>
      <!-- Advanced Panel -->
      <div v-if="filterOpen" class="border-t border-slate-200 p-4 space-y-3">
        <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">立案日期 從</label>
            <input v-model="filters.date_from" type="date" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm" />
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">立案日期 到</label>
            <input v-model="filters.date_to" type="date" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm" />
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">專案</label>
            <select v-model="filters.project_id" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm">
              <option :value="null">全部</option>
              <option v-for="p in meta.projects" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">客戶</label>
            <select v-model="filters.customer_id" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm">
              <option :value="null">全部</option>
              <option v-for="c in meta.customers" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">狀態</label>
            <select v-model="filters.status" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm">
              <option :value="null">全部</option>
              <option v-for="s in meta.enums.statuses" :key="s.value" :value="s.value">{{ s.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">類型</label>
            <select v-model="filters.case_type" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm">
              <option :value="null">全部</option>
              <option v-for="t in meta.enums.case_types" :key="t.value" :value="t.value">{{ t.label }}</option>
            </select>
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">處理 SE</label>
            <select v-model="filters.se_user_id" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm">
              <option :value="null">全部</option>
              <option v-for="u in meta.users.filter(u => u.role === 'SE')" :key="u.id" :value="u.id">{{ u.full_name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-[11px] font-medium text-slate-500 mb-1">立案人</label>
            <select v-model="filters.created_by" class="w-full h-9 px-2 rounded-lg border border-slate-300 text-sm">
              <option :value="null">全部</option>
              <option v-for="u in meta.users.filter(u => ['PM','SysAdmin'].includes(u.role))" :key="u.id" :value="u.id">{{ u.full_name }}</option>
            </select>
          </div>
        </div>
        <div class="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
          <template v-if="activeFilterChips.length > 0">
            <span class="text-xs text-slate-500 mr-1">已套用：</span>
            <span v-for="chip in activeFilterChips" :key="chip.key"
              class="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-indigo-50 text-indigo-700 text-xs">
              {{ chip.label }}：{{ chip.valueLabel }}
              <button @click="clearFilter(chip.key)" class="hover:text-indigo-900 ml-0.5">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </span>
            <div class="flex-1"></div>
          </template>
          <button @click="clearFilters" class="text-xs text-slate-500 hover:text-slate-700 underline">清除全部</button>
          <button @click="fetchCases(1)" class="h-8 px-3 rounded-lg bg-indigo-700 text-white text-xs hover:bg-indigo-800">套用</button>
        </div>
      </div>
    </section>

    <div v-if="selectedCount > 0" class="mb-3 rounded-xl bg-brand-700 text-white px-4 py-2.5 flex items-center gap-3 text-sm shadow-sm">
      <span class="tabular-nums">{{ selectedCount }} 件已選取</span>
      <div class="flex-1"></div>
      <button v-if="auth.role !== 'SE'" type="button" @click="openAssignModal" class="px-2.5 h-8 rounded-md bg-white/10 hover:bg-white/20 inline-flex items-center gap-1.5">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h6m6-5v6m3-3h-6"/></svg>
        批次派工
      </button>
      <button type="button" @click="exportSelectedCases" class="px-2.5 h-8 rounded-md bg-white/10 hover:bg-white/20 inline-flex items-center gap-1.5">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/></svg>
        匯出
      </button>
      <button type="button" @click="clearSelection" class="p-1.5 hover:bg-white/10 rounded">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
      </button>
    </div>

    <!-- Table Section -->
    <section class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-sm">
      <div class="flex items-center justify-between gap-2 px-4 py-2.5 border-b border-slate-200 text-xs">
        <div class="text-slate-600">共 <span class="tabular-nums font-semibold text-slate-900">{{ totalCount }}</span> 筆 · 顯示 <span class="tabular-nums">{{ displayRangeStart }}–{{ displayRangeEnd }}</span></div>
        <div class="flex items-center gap-2 text-slate-500">
          <span class="hidden sm:inline-flex items-center gap-1 tabular-nums"><svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>手動更新</span>
          <span class="hidden sm:inline">每頁</span>
          <select v-model="pageSize" @change="fetchCases(1)" class="h-8 px-2 rounded border border-slate-300 text-sm">
            <option :value="20">20</option>
            <option :value="50">50</option>
            <option :value="100">100</option>
          </select>
          <span v-if="loading" class="text-slate-400">載入中...</span>
        </div>
      </div>

      <!-- Desktop Table -->
      <div class="hidden md:block overflow-x-auto">
        <table class="min-w-[860px] w-full text-sm">
          <thead class="bg-slate-50 text-slate-600 text-left">
            <tr class="[&_th]:px-3 [&_th]:py-2.5 [&_th]:font-medium [&_th]:whitespace-nowrap">
              <th class="min-w-[150px] sticky left-0 bg-slate-50 z-10">
                <label class="inline-flex items-center gap-2 cursor-pointer">
                  <input :checked="allSelectedOnPage" type="checkbox" class="w-3.5 h-3.5 rounded border-slate-300 text-brand-600" @change="toggleSelectAllPage" />
                  <span>案件編號</span>
                  <svg class="w-3 h-3 inline text-slate-400 ml-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16V4m0 0L3 8m4-4l4 4M17 8v12m0 0l4-4m-4 4l-4-4"/></svg>
                </label>
              </th>
              <th class="min-w-[165px]">專案</th>
              <th class="min-w-[100px]">客戶</th>
              <th class="min-w-[90px]">類型</th>
              <th class="min-w-[90px]">狀態</th>
              <th class="min-w-[100px]">立案人</th>
              <th class="min-w-[160px]">SE</th>
              <th class="min-w-[120px]">
                最後更新
                <svg class="w-3 h-3 inline text-slate-600 ml-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 [&_td]:px-3 [&_td]:py-3 [&_td]:align-top">
            <tr v-for="c in cases" :key="c.id"
              class="bg-white hover:bg-slate-50 transition cursor-pointer group"
              :class="selectedIds.includes(c.id) ? 'selected-row' : ''"
              @click="$router.push(`/cases/${c.id}`)">
              <td class="sticky left-0 bg-white group-hover:bg-slate-50 transition z-[1]">
                <div class="flex items-center gap-2 flex-wrap">
                  <input :checked="selectedIds.includes(c.id)" type="checkbox" class="w-3.5 h-3.5 rounded border-slate-300 text-brand-600" @click.stop @change="toggleRowSelection(c.id)" />
                  <span class="tabular-nums text-indigo-700 font-medium text-[13px]">{{ c.case_number }}</span>
                  <span v-if="slaText(c)" class="text-[10px] px-1.5 py-0.5 rounded whitespace-nowrap"
                    :class="slaUrgent(c) ? 'bg-rose-50 text-rose-600 ring-1 ring-rose-200' : 'bg-amber-50 text-amber-700 ring-1 ring-amber-200'">
                    {{ slaText(c) }}
                  </span>
                </div>
              </td>
              <td>
                <div class="text-[13px] font-medium text-slate-900 truncate max-w-[200px]">{{ c.project?.code }}</div>
                <div class="text-xs text-slate-500">{{ c.project?.name }}</div>
              </td>
              <td class="text-[13px] text-slate-700">{{ c.customer?.name }}</td>
              <td>
                <span class="inline-flex items-center px-1.5 py-0.5 rounded-md text-[11px] font-medium"
                  :class="meta.caseTypeMap[c.case_type]?.color || 'bg-slate-100 text-slate-600'">
                  {{ meta.caseTypeMap[c.case_type]?.label || c.case_type }}
                </span>
              </td>
              <td>
                <span class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-md text-[11px] font-medium"
                  :class="meta.statusMap[c.status]?.color || 'bg-slate-100 text-slate-600'">
                  <span class="w-1.5 h-1.5 rounded-full" :class="statusDot(c.status)"></span>
                  {{ meta.statusMap[c.status]?.label || c.status }}
                </span>
              </td>
              <td class="text-[13px] text-slate-700">{{ c.created_by?.full_name || '—' }}</td>
              <td>
                <SeChips :ses="activeSEs(c)" />
              </td>
              <td class="text-[12px] text-slate-500 tabular-nums whitespace-nowrap">{{ relativeTime(c.updated_at) }}</td>
            </tr>
            <tr v-if="cases.length === 0 && !loading">
              <td colspan="8" class="p-8 text-center text-slate-400">暫無案件</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Mobile Cards -->
      <div class="md:hidden divide-y divide-slate-100">
        <router-link v-for="c in cases" :key="c.id" :to="`/cases/${c.id}`"
          class="block p-4 hover:bg-slate-50 active:bg-slate-100">
          <div class="flex items-start justify-between gap-2 mb-1.5">
            <div class="flex items-center gap-1.5 flex-wrap">
              <span class="tabular-nums text-[13px] text-indigo-700 font-semibold">{{ c.case_number }}</span>
              <span v-if="slaText(c)" class="text-[10px] px-1.5 py-0.5 rounded"
                :class="slaUrgent(c) ? 'bg-rose-50 text-rose-600 ring-1 ring-rose-200' : 'bg-amber-50 text-amber-700 ring-1 ring-amber-200'">
                {{ slaText(c) }}
              </span>
            </div>
            <span class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-md text-[11px] font-medium shrink-0"
              :class="meta.statusMap[c.status]?.color || 'bg-slate-100 text-slate-600'">
              {{ meta.statusMap[c.status]?.label || c.status }}
            </span>
          </div>
          <div class="text-sm font-medium text-slate-900">{{ c.project?.name }}</div>
          <div class="text-xs text-slate-500 mb-2">{{ c.customer?.name }}</div>
          <div class="flex items-center gap-2">
            <span class="inline-flex items-center px-1.5 py-0.5 rounded-md text-[11px] font-medium"
              :class="meta.caseTypeMap[c.case_type]?.color || 'bg-slate-100 text-slate-600'">
              {{ meta.caseTypeMap[c.case_type]?.label || c.case_type }}
            </span>
            <div class="flex-1"></div>
            <div class="text-[11px] text-slate-400">{{ relativeTime(c.updated_at) }}</div>
          </div>
        </router-link>
        <div v-if="cases.length === 0 && !loading" class="p-8 text-center text-slate-400">暫無案件</div>
      </div>

      <!-- Pagination -->
      <div class="flex flex-col sm:flex-row items-center justify-between gap-2 px-4 py-3 border-t border-slate-200 text-sm">
        <div class="text-xs text-slate-500 tabular-nums">
          顯示 {{ totalCount === 0 ? 0 : (page - 1) * pageSize + 1 }} – {{ Math.min(page * pageSize, totalCount) }} / {{ totalCount }} 筆
        </div>
        <nav class="inline-flex items-center gap-1">
          <button @click="fetchCases(page - 1)" :disabled="page <= 1"
            class="h-8 w-8 grid place-items-center rounded-md border border-slate-200 text-slate-400 disabled:opacity-40 hover:bg-slate-50 disabled:cursor-not-allowed">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
          </button>
          <template v-for="p in pageNumbers" :key="p">
            <span v-if="p === '...'" class="px-1 text-slate-400 text-xs">...</span>
            <button v-else @click="fetchCases(p)"
              class="h-8 w-8 rounded-md tabular-nums text-xs"
              :class="p === page ? 'bg-indigo-700 text-white' : 'hover:bg-slate-100'">{{ p }}</button>
          </template>
          <button @click="fetchCases(page + 1)" :disabled="page >= totalPages"
            class="h-8 w-8 grid place-items-center rounded-md border border-slate-200 text-slate-400 disabled:opacity-40 hover:bg-slate-50 disabled:cursor-not-allowed">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          </button>
        </nav>
      </div>
    </section>

    <p class="text-xs text-slate-400 mt-2">
      <span class="inline-block px-2 py-0.5 rounded bg-amber-50 text-amber-700 ring-1 ring-amber-200 text-[10px] mr-1.5">原型註記</span>
      P-04 案件列表 · 欄位改為「立案人 / SE」，多位 SE 以 +N 顯示，批次操作列依勾選結果出現。
    </p>

    <div v-if="assignModalOpen" class="fixed inset-0 z-50 bg-slate-950/45 flex items-center justify-center p-4" @click.self="closeAssignModal">
      <div class="w-full max-w-2xl rounded-2xl bg-white border border-slate-200 shadow-xl overflow-hidden">
        <div class="px-5 py-4 border-b border-slate-100 flex items-center justify-between gap-3">
          <div>
            <h2 class="text-lg font-semibold text-slate-900">批次派工</h2>
            <p class="text-xs text-slate-500 mt-0.5">將 {{ selectedCount }} 件案件指派給一位或多位 SE。</p>
          </div>
          <button type="button" @click="closeAssignModal" class="p-2 rounded-lg text-slate-400 hover:bg-slate-100 hover:text-slate-700">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>

        <div class="px-5 py-5 space-y-4">
          <div>
            <div class="text-sm font-medium text-slate-700 mb-2">選擇處理 SE</div>
            <div class="grid gap-2 sm:grid-cols-2 max-h-64 overflow-y-auto">
              <label v-for="user in seOptions" :key="user.id" class="flex items-start gap-3 rounded-xl border border-slate-200 p-3 hover:border-brand-300 hover:bg-brand-50/40 cursor-pointer">
                <input :checked="assignForm.seUserIds.includes(user.id)" type="checkbox" class="mt-0.5 w-4 h-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500" @change="toggleAssignSe(user.id)" />
                <span class="flex-1 min-w-0">
                  <span class="block text-sm font-medium text-slate-900">{{ user.full_name }}</span>
                  <span class="block text-xs text-slate-500 tabular-nums">{{ user.username || user.id }}</span>
                </span>
                <span v-if="assignForm.primarySeUserId === user.id" class="text-[10px] px-1.5 py-0.5 rounded-full bg-brand-100 text-brand-700">主要</span>
              </label>
            </div>
          </div>

          <div v-if="assignForm.seUserIds.length > 1">
            <label class="block text-sm font-medium text-slate-700 mb-1.5">主要負責 SE</label>
            <select v-model="assignForm.primarySeUserId" class="w-full h-10 px-3 rounded-lg border border-slate-300 bg-white text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500">
              <option v-for="user in selectedSeUsers" :key="user.id" :value="user.id">{{ user.full_name }}</option>
            </select>
          </div>

          <div class="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-end">
            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1.5">派工說明</label>
              <textarea v-model.trim="assignForm.instructions" rows="3" class="w-full px-3 py-2.5 rounded-lg border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500" placeholder="補充案件背景、注意事項或希望處理順序"></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1.5">預計完成日</label>
              <input v-model="assignForm.expectedCompletionDate" type="date" class="w-full h-10 px-3 rounded-lg border border-slate-300 text-sm tabular-nums focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500" />
            </div>
          </div>

          <div v-if="assignError" class="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ assignError }}</div>
        </div>

        <div class="px-5 py-4 border-t border-slate-100 flex items-center justify-end gap-2">
          <button type="button" @click="closeAssignModal" class="h-9 px-4 rounded-lg border border-slate-300 bg-white text-sm text-slate-700 hover:bg-slate-50">取消</button>
          <button type="button" @click="submitBatchAssign" :disabled="assignSaving" class="h-9 px-4 rounded-lg bg-brand-700 hover:bg-brand-800 disabled:bg-slate-300 text-sm font-medium text-white">
            {{ assignSaving ? '派工中…' : '確認派工' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useMetaStore } from '../stores/meta'
import { useAuthStore } from '../stores/auth'
import api from '../utils/api'
import SeChips from '../components/SeChips.vue'

defineOptions({
  name: 'CaseListView'
})

const route = useRoute()
const meta = useMetaStore()
const auth = useAuthStore()

const cases = ref([])
const page = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const totalPages = ref(1)
const loading = ref(false)
const filterOpen = ref(false)
const activeTab = ref('all')
const selectedIds = ref([])
const assignModalOpen = ref(false)
const assignSaving = ref(false)
const assignError = ref('')
const savedViewStorageKey = 'caseflow-case-list-saved-views'
const savedViews = ref(loadSavedViews())
const assignForm = ref(createAssignForm())

function emptyFilters() {
  return {
    q: null,
    project_id: null,
    customer_id: null,
    status: null,
    case_type: null,
    date_from: null,
    date_to: null,
    se_user_id: null,
    created_by: null
  }
}

const filters = ref(emptyFilters())

const baseQuickTabs = [
  { key: 'all', label: '全部' },
  { key: 'mine', label: '指派給我' },
  { key: 'created', label: '我立案' },
  { key: 'open', label: '未結案' }
]

const quickTabs = computed(() => [
  ...baseQuickTabs,
  ...savedViews.value.map(view => ({ key: view.key, label: view.label, saved: true }))
])

const authRoleLabel = computed(() => {
  const r = auth.role
  if (r === 'ADMIN' || r === 'SysAdmin') return 'SysAdmin'
  if (r === 'PM') return 'PM'
  if (r === 'SE') return 'SE'
  return r || '使用者'
})

const scopeHint = computed(() => {
  const r = auth.role
  if (r === 'SE') return '僅顯示指派給您的案件'
  if (r === 'PM') return '僅顯示所屬專案的案件'
  return '可見系統全部案件'
})

const roleBadgeText = computed(() => {
  if (auth.role === 'SE') return `我的案件 ${totalCount.value} 件`
  if (auth.role === 'PM') return `專案內 ${totalCount.value} 件`
  return `全系統 ${totalCount.value} 件`
})

const selectedCount = computed(() => selectedIds.value.length)
const displayRangeStart = computed(() => (totalCount.value === 0 ? 0 : (page.value - 1) * pageSize.value + 1))
const displayRangeEnd = computed(() => Math.min(page.value * pageSize.value, totalCount.value))
const visibleIds = computed(() => cases.value.map(item => item.id))
const allSelectedOnPage = computed(() => visibleIds.value.length > 0 && visibleIds.value.every(id => selectedIds.value.includes(id)))
const seOptions = computed(() => meta.users.filter(user => user.role === 'SE'))
const selectedSeUsers = computed(() => seOptions.value.filter(user => assignForm.value.seUserIds.includes(user.id)))
const activeScopeKey = computed(() => {
  const saved = savedViews.value.find(view => view.key === activeTab.value)
  return saved?.sourceTab || activeTab.value
})

function cloneFilters(source = {}) {
  return {
    ...emptyFilters(),
    ...source
  }
}

function loadSavedViews() {
  try {
    const raw = localStorage.getItem(savedViewStorageKey)
    const parsed = JSON.parse(raw || '[]')
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function persistSavedViews() {
  localStorage.setItem(savedViewStorageKey, JSON.stringify(savedViews.value))
}

function createAssignForm() {
  return {
    seUserIds: [],
    primarySeUserId: null,
    instructions: '',
    expectedCompletionDate: ''
  }
}

const activeFilterCount = computed(() => {
  return Object.values(filters.value).filter(v => v !== null && v !== '').length
})

const activeFilterChips = computed(() => {
  const chips = []
  const f = filters.value
  const filterLabelMap = {
    q: '關鍵字',
    date_from: '立案日 從',
    date_to: '立案日 到',
    project_id: '專案',
    customer_id: '客戶',
    status: '狀態',
    case_type: '類型',
    se_user_id: '處理SE',
    created_by: '立案人'
  }
  if (f.q) chips.push({ key: 'q', label: filterLabelMap.q, valueLabel: f.q })
  if (f.date_from) chips.push({ key: 'date_from', label: filterLabelMap.date_from, valueLabel: f.date_from })
  if (f.date_to) chips.push({ key: 'date_to', label: filterLabelMap.date_to, valueLabel: f.date_to })
  if (f.project_id) {
    const p = meta.projects?.find(x => x.id === f.project_id)
    chips.push({ key: 'project_id', label: filterLabelMap.project_id, valueLabel: p ? `${p.code} ${p.name}` : f.project_id })
  }
  if (f.customer_id) {
    const c = meta.customers?.find(x => x.id === f.customer_id)
    chips.push({ key: 'customer_id', label: filterLabelMap.customer_id, valueLabel: c ? c.name : f.customer_id })
  }
  if (f.status !== null && f.status !== undefined && f.status !== '') {
    const s = meta.enums?.statuses?.find(x => x.value === f.status)
    chips.push({ key: 'status', label: filterLabelMap.status, valueLabel: s ? s.label : f.status })
  }
  if (f.case_type) {
    chips.push({ key: 'case_type', label: filterLabelMap.case_type, valueLabel: meta.caseTypeMap[f.case_type]?.label || f.case_type })
  }
  if (f.se_user_id) {
    const u = meta.users?.find(x => x.id === f.se_user_id)
    chips.push({ key: 'se_user_id', label: filterLabelMap.se_user_id, valueLabel: u ? u.full_name : f.se_user_id })
  }
  if (f.created_by) {
    const u = meta.users?.find(x => x.id === f.created_by)
    chips.push({ key: 'created_by', label: filterLabelMap.created_by, valueLabel: u ? u.full_name : f.created_by })
  }
  return chips
})

const pageNumbers = computed(() => {
  const total = totalPages.value
  const cur = page.value
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)
  const pages = []
  pages.push(1)
  if (cur > 3) pages.push('...')
  for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) pages.push(i)
  if (cur < total - 2) pages.push('...')
  pages.push(total)
  return pages
})

function switchTab(key) {
  const saved = savedViews.value.find(view => view.key === key)
  if (saved) {
    activeTab.value = key
    filters.value = cloneFilters(saved.filters)
    filterOpen.value = Object.values(filters.value).some(value => value !== null && value !== '')
    clearSelection()
    fetchCases(1)
    return
  }

  activeTab.value = key
  filters.value = emptyFilters()
  filterOpen.value = false
  clearSelection()
  fetchCases(1)
}

function clearFilters() {
  filters.value = emptyFilters()
  clearSelection()
  fetchCases(1)
}

function clearFilter(key) {
  filters.value[key] = null
  clearSelection()
  fetchCases(1)
}

function toggleRowSelection(id) {
  if (selectedIds.value.includes(id)) {
    selectedIds.value = selectedIds.value.filter(item => item !== id)
  } else {
    selectedIds.value = [...selectedIds.value, id]
  }
}

function toggleSelectAllPage() {
  if (allSelectedOnPage.value) {
    selectedIds.value = selectedIds.value.filter(id => !visibleIds.value.includes(id))
    return
  }
  selectedIds.value = Array.from(new Set([...selectedIds.value, ...visibleIds.value]))
}

function clearSelection() {
  selectedIds.value = []
}

function openAssignModal() {
  assignError.value = ''
  assignForm.value = createAssignForm()
  assignModalOpen.value = true
}

function closeAssignModal() {
  assignModalOpen.value = false
  assignSaving.value = false
  assignError.value = ''
}

function toggleAssignSe(userId) {
  const exists = assignForm.value.seUserIds.includes(userId)
  if (exists) {
    assignForm.value.seUserIds = assignForm.value.seUserIds.filter(id => id !== userId)
    if (assignForm.value.primarySeUserId === userId) {
      assignForm.value.primarySeUserId = assignForm.value.seUserIds[0] || null
    }
    return
  }

  assignForm.value.seUserIds = [...assignForm.value.seUserIds, userId]
  if (!assignForm.value.primarySeUserId) {
    assignForm.value.primarySeUserId = userId
  }
}

function activeSEs(c) {
  return c.assigned_ses || []
}

function slaText(c) {
  if (!c.due_at) return null
  const ms = new Date(c.due_at) - Date.now()
  if (ms < 0) return '已逾期'
  const hrs = Math.floor(ms / 3600000)
  if (hrs < 24) return `剩 ${hrs}h`
  const days = Math.floor(hrs / 24)
  if (days <= 3) return `剩 ${days}d`
  return null
}

function slaUrgent(c) {
  if (!c.due_at) return false
  const ms = new Date(c.due_at) - Date.now()
  return ms < 0 || ms < 24 * 3600000
}

function statusDot(status) {
  const map = { 10: 'bg-slate-400', 20: 'bg-sky-500', 30: 'bg-amber-500', 35: 'bg-orange-500', 40: 'bg-emerald-500', 50: 'bg-slate-400', 60: 'bg-rose-400' }
  return map[status] || 'bg-slate-300'
}

function relativeTime(dt) {
  if (!dt) return ''
  const ms = Date.now() - new Date(dt)
  const mins = Math.floor(ms / 60000)
  if (mins < 1) return '剛剛'
  if (mins < 60) return `${mins} 分鐘前`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs} 小時前`
  const days = Math.floor(hrs / 24)
  if (days < 3) return `${days} 天前`
  return new Date(dt).toLocaleDateString('zh-TW', { month: '2-digit', day: '2-digit' })
}

function toNullableInt(value) {
  if (value === undefined || value === null || value === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

function applyInitialQuery() {
  const query = route.query
  const nextFilters = emptyFilters()

  if (typeof query.q === 'string' && query.q.trim()) nextFilters.q = query.q.trim()

  const status = toNullableInt(query.status)
  if (status !== null) nextFilters.status = status

  const projectId = toNullableInt(query.project_id)
  if (projectId !== null) nextFilters.project_id = projectId

  const customerId = toNullableInt(query.customer_id)
  if (customerId !== null) nextFilters.customer_id = customerId

  if (typeof query.case_type === 'string' && query.case_type) nextFilters.case_type = query.case_type

  filters.value = nextFilters

  if (typeof query.tab === 'string' && quickTabs.value.some(tab => tab.key === query.tab)) {
    activeTab.value = query.tab
  }

  filterOpen.value = Object.values(nextFilters).some(value => value !== null && value !== '')
}

async function fetchCases(p = page.value) {
  loading.value = true
  page.value = p
  try {
    const params = { page: p, page_size: pageSize.value }
    if (activeScopeKey.value === 'mine') params.assigned_to_me = true
    if (activeScopeKey.value === 'created') params.created_by_me = true
    if (activeScopeKey.value === 'open') params.open_only = true
    Object.entries(filters.value).forEach(([k, v]) => { if (v) params[k] = v })
    const { data: res } = await api.get('/cases', { params })
    if (res.success) {
      cases.value = res.data
      totalCount.value = res.meta.total
      totalPages.value = Math.max(1, Math.ceil(res.meta.total / pageSize.value))
      selectedIds.value = selectedIds.value.filter(id => res.data.some(item => item.id === id))
    }
  } finally {
    loading.value = false
  }
}

function saveCurrentView() {
  const label = window.prompt('請輸入檢視名稱', '自訂檢視')
  if (!label?.trim()) return

  const view = {
    key: `saved-${Date.now()}`,
    label: label.trim(),
    sourceTab: activeScopeKey.value,
    filters: cloneFilters(filters.value)
  }

  savedViews.value = [...savedViews.value, view]
  persistSavedViews()
  activeTab.value = view.key
}

async function submitBatchAssign() {
  assignError.value = ''
  if (assignForm.value.seUserIds.length === 0) {
    assignError.value = '請至少選擇一位 SE'
    return
  }

  assignSaving.value = true
  try {
    const payload = {
      seUserIds: assignForm.value.seUserIds,
      primarySeUserId: assignForm.value.primarySeUserId || assignForm.value.seUserIds[0],
      instructions: assignForm.value.instructions || null,
      expectedCompletionDate: assignForm.value.expectedCompletionDate || null
    }

    await Promise.all(selectedIds.value.map(caseId => api.post(`/cases/${caseId}/assign`, payload)))
    closeAssignModal()
    clearSelection()
    await fetchCases(page.value)
  } catch (error) {
    assignError.value = error?.response?.data?.error?.message || '批次派工失敗，請稍後再試'
  } finally {
    assignSaving.value = false
  }
}

function exportSelectedCases() {
  const rows = cases.value.filter(item => selectedIds.value.includes(item.id))
  if (rows.length === 0) return
  const header = '案件編號,專案,客戶,狀態,最後更新\n'
  const body = rows.map(item => {
    const projectName = `${item.project?.code || ''} ${item.project?.name || ''}`.trim()
    const statusText = meta.statusMap[item.status]?.label || item.status
    return [item.case_number, projectName, item.customer?.name || '', statusText, relativeTime(item.updated_at)].join(',')
  }).join('\n')
  const blob = new Blob(['\uFEFF' + header + body], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `cases_selected_${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  URL.revokeObjectURL(url)
}

async function exportCases() {
  try {
    const payload = {
      report_type: 'hours',
      project_id: filters.value.project_id || null,
      date_from: filters.value.date_from || null,
      date_to: filters.value.date_to || null
    }
    const res = await api.post('/reports/export', payload, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `cases_export_${new Date().toISOString().slice(0, 10)}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
  } catch {
    alert('匯出失敗，請稍後再試')
  }
}

onMounted(async () => {
  if (!meta.loaded) {
    await meta.fetchDropdowns()
  }
  applyInitialQuery()
  fetchCases()
})
</script>

<style scoped>
@reference "tailwindcss";

.thin-scroll::-webkit-scrollbar {
  height: 8px;
}

.thin-scroll::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 9999px;
}

.selected-row td {
  background: #eef2ff;
}
</style>