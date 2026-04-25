<template>
  <!-- Main content (when caseData loaded) -->
  <div class="space-y-5 pb-10" v-if="caseData">

    <!-- Breadcrumb -->
    <nav class="flex items-center gap-1.5 text-xs text-slate-500">
      <router-link to="/dashboard" class="hover:text-slate-800">儀表板</router-link>
      <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
      <router-link to="/cases" class="hover:text-slate-800">案件列表</router-link>
      <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
      <span class="text-slate-800 font-medium">{{ caseData.case_number }}</span>
    </nav>

    <!-- Closed / Cancelled status banner -->
    <div v-if="[50,60].includes(caseData.status)"
      class="flex items-center gap-3 px-4 py-3 rounded-xl border text-sm font-medium"
      :class="caseData.status === 50 ? 'bg-emerald-50 border-emerald-200 text-emerald-800' : 'bg-slate-100 border-slate-200 text-slate-600'">
      <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
      </svg>
      {{ caseData.status === 50 ? '此案件已結案' : '此案件已取消' }}
    </div>

    <!-- Header card -->
    <div class="bg-white border border-slate-200 rounded-xl shadow-card p-5 md:p-6">
      <div class="flex items-start gap-5">
        <!-- Left info -->
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2 flex-wrap mb-2">
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
              :class="meta.statusMap[caseData.status]?.color || 'bg-slate-100 text-slate-700'">
              {{ meta.statusMap[caseData.status]?.label }}
            </span>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
              :class="meta.caseTypeMap[caseData.case_type]?.color || 'bg-blue-50 text-blue-700'">
              {{ meta.caseTypeMap[caseData.case_type]?.label }}
            </span>
            <span v-if="slaBadge"
              class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
              :class="slaUrgent ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-800'">
              {{ slaBadge }}
            </span>
            <router-link v-if="caseData.related_case" :to="`/cases/${caseData.related_case.id}`"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs bg-indigo-50 text-indigo-700 hover:underline">
              ↩ {{ caseData.related_case.case_number }}
            </router-link>
          </div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900">{{ caseData.case_number }}</h1>
          <p class="text-sm text-slate-500 mt-1 leading-relaxed">
            {{ caseData.description?.substring(0, 120) }}<span v-if="(caseData.description?.length || 0) > 120">…</span>
          </p>
          <div class="flex flex-wrap items-center gap-x-3 gap-y-1.5 mt-3 text-xs text-slate-500">
            <span class="inline-flex items-center gap-1">
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7l3 9 6-4 6 4 3-9H3z"/></svg>
              {{ caseData.project?.code }} {{ caseData.project?.name }}
            </span>
            <span>·</span>
            <span class="inline-flex items-center gap-1">
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-2 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/></svg>
              {{ caseData.customer?.name }}
            </span>
            <template v-if="caseData.category">
              <span>·</span>
              <span class="inline-flex items-center gap-1">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"/></svg>
                {{ caseData.category?.name }}
              </span>
            </template>
          </div>
        </div>

        <!-- Right people sidebar -->
        <aside class="hidden md:block w-60 shrink-0 bg-slate-50 border border-slate-200 rounded-xl p-3.5">
          <p class="text-[11px] font-semibold text-slate-400 uppercase tracking-wide mb-2">關係人</p>
          <dl class="space-y-2 text-[13px]">
            <div class="flex items-start gap-1.5">
              <dt class="w-14 shrink-0 text-slate-400">立案人</dt>
              <dd class="text-slate-700 font-medium">{{ caseData.created_by?.full_name || '—' }}</dd>
            </div>
            <div class="flex items-start gap-1.5">
              <dt class="w-14 shrink-0 text-slate-400">轉派 PM</dt>
              <dd class="text-slate-700 font-medium">{{ caseData.assigned_pm?.full_name || '—' }}</dd>
            </div>
            <div class="flex items-start gap-1.5">
              <dt class="w-14 shrink-0 text-slate-400">處理 SE</dt>
              <dd class="text-slate-700">
                <template v-if="activeAssignments.length">
                  <span v-for="(a, i) in activeAssignments" :key="a.id" class="font-medium">
                    {{ a.se?.full_name }}<span v-if="i < activeAssignments.length - 1">、</span>
                  </span>
                </template>
                <span v-else class="text-slate-400">未指派</span>
              </dd>
            </div>
          </dl>
          <div class="mt-3 pt-3 border-t border-slate-200 space-y-1.5 text-[11px] text-slate-500">
            <div class="flex gap-1">
              <span class="w-14 shrink-0">立案時間</span>
              <span>{{ caseData.created_at ? new Date(caseData.created_at).toLocaleString('zh-TW', {month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}) : '—' }}</span>
            </div>
            <div v-if="caseData.due_at" class="flex gap-1">
              <span class="w-14 shrink-0">SLA 截止</span>
              <span :class="slaUrgent ? 'text-rose-600 font-semibold' : ''">{{ new Date(caseData.due_at).toLocaleString('zh-TW', {month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}) }}</span>
            </div>
            <div v-if="caseData.closed_at" class="flex gap-1">
              <span class="w-14 shrink-0">結案時間</span>
              <span>{{ new Date(caseData.closed_at).toLocaleString('zh-TW', {month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}) }}</span>
            </div>
          </div>
        </aside>
      </div>
    </div>

    <!-- Action bar -->
    <div v-if="availableActions.length" class="rounded-xl border border-amber-200 bg-amber-50/60 px-4 py-2.5 flex items-center gap-3 flex-wrap">
      <div class="flex items-center gap-1.5 text-xs text-amber-800">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/></svg>
        <span class="font-medium">可執行動作</span>
      </div>
      <div class="flex-1"></div>
      <div class="flex flex-wrap gap-2">
        <button v-for="action in availableActions" :key="action.label"
          @click="action.handler()"
          class="h-8 px-3.5 rounded-lg text-xs font-medium transition"
          :class="action.class">
          {{ action.label }}
        </button>
      </div>
    </div>

    <!-- Tabs container -->
    <div class="bg-white border border-slate-200 rounded-xl shadow-card overflow-hidden">
      <div class="border-b border-slate-200 overflow-x-auto">
        <nav class="flex -mb-px px-1">
          <button v-for="tab in tabs" :key="tab.key" @click="activeTab = tab.key"
            class="px-4 py-3.5 text-sm whitespace-nowrap border-b-2 transition-colors"
            :class="activeTab === tab.key
              ? 'border-brand-700 text-brand-800 font-semibold'
              : 'border-transparent text-slate-500 hover:text-slate-700 hover:bg-slate-50'">
            {{ tab.label }}
            <span v-if="tab.count != null && tab.count > 0" class="ml-1 text-[11px] bg-slate-100 rounded-full px-1.5 py-0.5 tabular-nums">{{ tab.count }}</span>
          </button>
        </nav>
      </div>

      <div class="p-5 md:p-6">

        <!-- Tab: 基本資訊 -->
        <div v-if="activeTab === 'info'" class="space-y-5">
          <div class="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-4">
            <div><p class="text-xs text-slate-400 mb-0.5">問題分類</p><p class="text-sm text-slate-900">{{ caseData.category?.name || '—' }}</p></div>
            <div><p class="text-xs text-slate-400 mb-0.5">系統/模組</p><p class="text-sm text-slate-900">{{ caseData.module?.name || '—' }}</p></div>
            <div><p class="text-xs text-slate-400 mb-0.5">報修人</p><p class="text-sm text-slate-900">{{ caseData.reporter_name || '—' }}</p></div>
            <div><p class="text-xs text-slate-400 mb-0.5">聯絡電話</p><p class="text-sm text-slate-900">{{ caseData.reporter_phone || '—' }}</p></div>
            <div><p class="text-xs text-slate-400 mb-0.5">聯絡 Email</p><p class="text-sm text-slate-900">{{ caseData.reporter_email || '—' }}</p></div>
          </div>
          <div>
            <p class="text-xs text-slate-400 mb-1">問題描述</p>
            <div class="text-sm text-slate-800 leading-relaxed whitespace-pre-wrap bg-slate-50 rounded-lg px-4 py-3 border border-slate-100">{{ caseData.description }}</div>
          </div>
          <div v-if="caseAttachments.length > 0">
            <p class="text-xs text-slate-400 mb-2">附件</p>
            <div class="space-y-2">
              <div v-for="att in caseAttachments" :key="att.id"
                class="flex items-center justify-between px-3 py-2 bg-slate-50 rounded-lg border border-slate-100">
                <div class="flex items-center gap-2 text-sm">
                  <svg class="w-4 h-4 text-slate-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13"/></svg>
                  <span class="font-medium text-slate-700">{{ att.file_name }}</span>
                  <span class="text-slate-400 text-xs">{{ formatFileSize(att.file_size) }}</span>
                </div>
                <a :href="`/api/v1/attachments/${att.id}/download`" class="text-brand-700 text-xs hover:underline">下載</a>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab: 處理歷程 -->
        <div v-if="activeTab === 'logs'" class="space-y-4">
          <div class="flex items-center justify-between flex-wrap gap-2">
            <div class="flex items-center gap-3 text-xs text-slate-500">
              <span>總工時 <strong class="text-slate-800 tabular-nums">{{ caseData.summary?.total_hours ?? 0 }}</strong> hr</span>
              <span>·</span>
              <span>共 <strong class="text-slate-800 tabular-nums">{{ caseData.logs?.length || 0 }}</strong> 筆紀錄</span>
            </div>
            <button @click="showLogForm = !showLogForm"
              class="h-8 px-3 rounded-lg text-xs font-medium bg-brand-700 text-white hover:bg-brand-800">
              + 新增紀錄
            </button>
          </div>
          <div v-if="showLogForm" class="bg-slate-50 border border-slate-200 rounded-xl p-4 space-y-3">
            <p class="text-xs font-semibold text-slate-600 uppercase tracking-wide">新增處理紀錄</p>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div><label class="label">處理日期 *</label><input v-model="logForm.log_date" type="date" class="input-base" /></div>
              <div><label class="label">工時 (hr) *</label><input v-model.number="logForm.hours_spent" type="number" step="0.5" min="0" class="input-base" /></div>
            </div>
            <div><label class="label">處理方式 *</label><textarea v-model="logForm.handling_method" rows="2" class="input-base"></textarea></div>
            <div><label class="label">處理結果</label><textarea v-model="logForm.handling_result" rows="2" class="input-base"></textarea></div>
            <div class="flex justify-end gap-2">
              <button @click="showLogForm = false" class="h-8 px-3 text-sm border border-slate-300 rounded-lg hover:bg-slate-50">取消</button>
              <button @click="submitLog" class="h-8 px-3 text-sm bg-brand-700 text-white rounded-lg hover:bg-brand-800">送出</button>
            </div>
          </div>
          <div class="divide-y divide-slate-100">
            <div v-for="log in (caseData.logs || [])" :key="log.id" class="py-4 first:pt-0">
              <div class="flex items-center gap-2 flex-wrap mb-1.5">
                <span class="inline-flex items-center gap-1 text-xs font-semibold text-slate-700 tabular-nums">
                  <svg class="w-3.5 h-3.5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
                  {{ log.log_date }}
                </span>
                <span class="text-xs text-slate-500">{{ log.handler?.full_name }}</span>
                <span class="ml-auto inline-flex items-center px-2 py-0.5 rounded-full text-[11px] bg-blue-50 text-blue-700 tabular-nums">{{ log.hours_spent }} hr</span>
              </div>
              <div class="border-l-2 border-brand-200 pl-3 space-y-1">
                <p class="text-sm"><span class="text-slate-400 text-xs">處理方式</span><br>{{ log.handling_method }}</p>
                <p v-if="log.handling_result" class="text-sm text-slate-600"><span class="text-slate-400 text-xs">處理結果</span><br>{{ log.handling_result }}</p>
              </div>
            </div>
            <div v-if="!(caseData.logs?.length)" class="py-8 text-center text-sm text-slate-400">尚無處理紀錄</div>
          </div>
        </div>

        <!-- Tab: 派工 -->
        <div v-if="activeTab === 'assign'" class="space-y-4">
          <div class="flex items-center justify-between flex-wrap gap-2">
            <p class="text-sm text-slate-500">目前指派給此案件的 SE 工程師</p>
            <button
              v-if="['PM','ADMIN','SysAdmin'].includes(auth.role) && ![50,60].includes(caseData?.status)"
              @click="openAssignModal"
              class="inline-flex items-center gap-1.5 h-8 px-3 rounded-lg text-xs font-medium bg-brand-700 text-white hover:bg-brand-800">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"/></svg>
              轉派 SE
            </button>
          </div>
          <div class="space-y-2">
            <div v-for="a in activeAssignments" :key="a.id"
              class="flex items-center gap-3 p-3 bg-slate-50 border border-slate-200 rounded-xl">
              <div class="w-8 h-8 rounded-full bg-brand-100 text-brand-700 flex items-center justify-center text-sm font-semibold shrink-0">
                {{ (a.se?.full_name || '?').charAt(0) }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-slate-900">{{ a.se?.full_name }}</p>
                <p class="text-xs text-slate-400">派工人：{{ a.assigned_by?.full_name }} · {{ a.assigned_at ? new Date(a.assigned_at).toLocaleDateString('zh-TW') : '' }}</p>
              </div>
              <span v-if="a.expected_completion_date" class="text-xs text-slate-400 whitespace-nowrap">預計 {{ a.expected_completion_date }}</span>
              <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200">處理中</span>
            </div>
            <div v-if="activeAssignments.length === 0" class="py-8 text-center text-sm text-slate-400">尚未指派工程師</div>
          </div>
          <div v-if="inactiveAssignments.length > 0">
            <p class="text-xs text-slate-400 mb-2">歷史派工紀錄</p>
            <div class="space-y-1">
              <div v-for="a in inactiveAssignments" :key="'ia'+a.id"
                class="flex items-center gap-3 p-2 rounded-lg opacity-50">
                <div class="w-7 h-7 rounded-full bg-slate-100 text-slate-500 flex items-center justify-center text-xs font-semibold">{{ (a.se?.full_name || '?').charAt(0) }}</div>
                <span class="text-xs text-slate-500">{{ a.se?.full_name }}</span>
                <span class="text-xs text-slate-400">{{ a.assigned_at ? new Date(a.assigned_at).toLocaleDateString('zh-TW') : '' }}</span>
                <span class="ml-auto text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-500">已解除</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab: 工時評估 -->
        <div v-if="activeTab === 'estimation'" class="space-y-4">
          <div class="flex items-center justify-between">
            <p class="text-sm text-slate-500">工時評估紀錄</p>
            <button @click="showEstForm = !showEstForm"
              class="h-8 px-3 rounded-lg text-xs font-medium bg-brand-700 text-white hover:bg-brand-800">
              + 新增評估
            </button>
          </div>
          <div v-if="showEstForm" class="bg-slate-50 border border-slate-200 rounded-xl p-4 space-y-3">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div><label class="label">提出日期 *</label><input v-model="estForm.request_date" type="date" class="input-base" /></div>
              <div><label class="label">評估工時 (hr) *</label><input v-model.number="estForm.estimated_hours" type="number" step="0.5" class="input-base" /></div>
              <div>
                <label class="label">評估人員 *</label>
                <select v-model="estForm.estimator_user_id" class="input-base">
                  <option v-for="u in meta.users" :key="u.id" :value="u.id">{{ u.full_name }} ({{ u.role }})</option>
                </select>
              </div>
            </div>
            <div><label class="label">概要 *</label><textarea v-model="estForm.summary" rows="2" class="input-base"></textarea></div>
            <div><label class="label">備註</label><textarea v-model="estForm.remarks" rows="2" class="input-base"></textarea></div>
            <div class="flex justify-end gap-2">
              <button @click="showEstForm = false" class="h-8 px-3 text-sm border border-slate-300 rounded-lg hover:bg-slate-50">取消</button>
              <button @click="submitEstimation" class="h-8 px-3 text-sm bg-brand-700 text-white rounded-lg hover:bg-brand-800">送出</button>
            </div>
          </div>
          <div v-if="(caseData.estimations || []).length > 0" class="overflow-x-auto rounded-xl border border-slate-200">
            <table class="min-w-full divide-y divide-slate-200">
              <thead class="bg-slate-50"><tr>
                <th class="th-cell">項次</th><th class="th-cell">提出日</th><th class="th-cell">概要</th>
                <th class="th-cell">評估工時</th><th class="th-cell">狀態</th><th class="th-cell">評估人員</th>
              </tr></thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="e in caseData.estimations" :key="e.id" class="hover:bg-slate-50/70">
                  <td class="td-cell tabular-nums">{{ e.seq_no }}</td>
                  <td class="td-cell tabular-nums">{{ e.request_date }}</td>
                  <td class="td-cell max-w-xs truncate">{{ e.summary }}</td>
                  <td class="td-cell tabular-nums">{{ e.estimated_hours }} hr</td>
                  <td class="td-cell"><span class="badge" :class="estStatusColor(e.estimation_status)">{{ estStatusLabel(e.estimation_status) }}</span></td>
                  <td class="td-cell">{{ e.estimator?.full_name }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else-if="!showEstForm" class="py-8 text-center text-sm text-slate-400">尚無工時評估紀錄</div>
        </div>

        <!-- Tab: 客戶回覆 -->
        <div v-if="activeTab === 'replies'" class="space-y-4">
          <div class="flex items-center justify-between">
            <p class="text-sm text-slate-500">客戶回覆紀錄</p>
            <button @click="showReplyForm = !showReplyForm"
              class="h-8 px-3 rounded-lg text-xs font-medium bg-brand-700 text-white hover:bg-brand-800">
              + 新增回覆
            </button>
          </div>
          <div v-if="showReplyForm" class="bg-slate-50 border border-slate-200 rounded-xl p-4 space-y-3">
            <div><label class="label">回覆日期</label><input v-model="replyForm.reply_date" type="date" class="input-base" /></div>
            <div><label class="label">回覆內容 *</label><textarea v-model="replyForm.reply_content" rows="4" class="input-base"></textarea></div>
            <div class="flex justify-end gap-2">
              <button @click="showReplyForm = false" class="h-8 px-3 text-sm border border-slate-300 rounded-lg hover:bg-slate-50">取消</button>
              <button @click="submitReply" class="h-8 px-3 text-sm bg-brand-700 text-white rounded-lg hover:bg-brand-800">送出</button>
            </div>
          </div>
          <div class="divide-y divide-slate-100">
            <div v-for="r in (caseData.replies || [])" :key="r.id" class="py-4 first:pt-0">
              <div class="flex items-center gap-2 mb-1.5">
                <span class="text-xs font-semibold text-slate-700 tabular-nums">{{ r.reply_date }}</span>
                <span class="text-xs text-slate-500">{{ r.replier?.full_name }}</span>
              </div>
              <div class="border-l-2 border-emerald-200 pl-3">
                <p class="text-sm text-slate-700 whitespace-pre-wrap">{{ r.reply_content }}</p>
              </div>
            </div>
            <div v-if="!(caseData.replies?.length)" class="py-8 text-center text-sm text-slate-400">尚無回覆紀錄</div>
          </div>
        </div>

      </div>
    </div>
  </div>

  <!-- Skeleton (loading) -->
  <div v-else class="space-y-5">
    <div class="h-8 w-48 bg-slate-200 rounded animate-pulse"></div>
    <div class="h-48 bg-white border border-slate-200 rounded-xl shadow-card animate-pulse"></div>
    <div class="h-64 bg-white border border-slate-200 rounded-xl shadow-card animate-pulse"></div>
  </div>

  <!-- Assign Modal -->
  <div v-if="assignModal.show" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" @click.self="assignModal.show = false">
    <div class="bg-white rounded-2xl shadow-2xl w-full max-w-lg flex flex-col max-h-[90vh]">
      <!-- modal header -->
      <div class="p-5 border-b border-slate-100 flex items-start gap-3">
        <div class="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-700 grid place-items-center flex-shrink-0">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"/></svg>
        </div>
        <div class="flex-1">
          <div class="text-[15px] font-semibold text-slate-900">轉派 SE</div>
          <div class="text-[12px] text-slate-500 mt-0.5">可勾選一位或多位成員共同處理此案件</div>
        </div>
        <button @click="assignModal.show = false" class="w-7 h-7 rounded-lg hover:bg-slate-100 text-slate-500 grid place-items-center">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
        </button>
      </div>
      <!-- modal body -->
      <div class="p-5 space-y-4 overflow-y-auto flex-1">
        <!-- 搜尋 -->
        <div class="relative">
          <svg class="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35m0 0A7 7 0 1 0 4.65 4.65a7 7 0 0 0 11.99 11.99z"/></svg>
          <input v-model="assignModal.search" type="text" placeholder="搜尋姓名…" class="w-full h-9 pl-8 pr-3 rounded-lg border border-slate-300 text-[13px] focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100" />
        </div>
        <!-- 已選標籤 -->
        <div v-if="assignModal.selectedIds.length" class="flex flex-wrap gap-1.5">
          <span v-for="id in assignModal.selectedIds" :key="id" class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-800 ring-1 ring-indigo-200 text-[11.5px]">
            {{ seNameById(id) }}
            <button type="button" @click="assignModal.selectedIds = assignModal.selectedIds.filter(x => x !== id)" class="text-indigo-400 hover:text-rose-500 ml-0.5">✕</button>
          </span>
        </div>
        <div v-else class="text-[12px] text-slate-400 italic">尚未選擇</div>
        <!-- SE 清單 -->
        <div class="border border-slate-200 rounded-xl max-h-64 overflow-y-auto divide-y divide-slate-100">
          <label
            v-for="se in filteredAssignSEs" :key="se.id"
            class="flex items-center gap-3 px-3 py-2.5 hover:bg-indigo-50/40 cursor-pointer"
          >
            <input type="checkbox" :value="se.id" v-model="assignModal.selectedIds" class="w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
            <div class="w-7 h-7 rounded-full bg-indigo-100 text-indigo-700 flex items-center justify-center text-xs font-semibold flex-shrink-0">{{ se.full_name.charAt(0) }}</div>
            <div class="flex-1 min-w-0 flex items-center gap-2">
              <span class="text-[13px] text-slate-900">{{ se.full_name }}</span>
              <span class="text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-600">SE</span>
              <span v-if="activeAssignmentIds.includes(se.id)" class="text-[10px] px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200">目前派工中</span>
            </div>
          </label>
          <div v-if="filteredAssignSEs.length === 0" class="py-6 text-center text-sm text-slate-400">找不到符合的成員</div>
        </div>
        <!-- 備註 -->
        <div>
          <label class="block text-[12px] font-medium text-slate-600 mb-1.5">處理指示（選填）</label>
          <textarea v-model="assignModal.instructions" rows="2" class="w-full px-3 py-2 rounded-lg border border-slate-300 text-[13px] focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100" placeholder="給 SE 的補充說明…"></textarea>
        </div>
        <!-- 預計完成日 -->
        <div>
          <label class="block text-[12px] font-medium text-slate-600 mb-1.5">預計完成日（選填）</label>
          <input v-model="assignModal.expectedDate" type="date" class="w-full h-9 px-3 rounded-lg border border-slate-300 text-[13px] focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100" />
        </div>
        <!-- 錯誤訊息 -->
        <p v-if="assignModal.error" class="text-sm text-rose-600">{{ assignModal.error }}</p>
      </div>
      <!-- modal footer -->
      <div class="px-5 py-3.5 bg-slate-50 border-t border-slate-100 flex items-center justify-end gap-2">
        <button @click="assignModal.show = false" class="h-9 px-4 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-slate-700 text-[13px]">取消</button>
        <button
          @click="submitAssign"
          :disabled="assignModal.selectedIds.length === 0 || assignModal.submitting"
          class="h-9 px-4 rounded-lg bg-indigo-700 hover:bg-indigo-800 disabled:bg-slate-300 disabled:cursor-not-allowed text-white text-[13px] font-medium inline-flex items-center gap-1.5"
        >
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
          {{ assignModal.submitting ? '派工中…' : '確認派工' }}
        </button>
      </div>
    </div>
  </div>

  <!-- Confirm Dialog -->
  <div v-if="confirmDialog.show" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
    <div class="bg-white rounded-xl p-6 max-w-md w-full mx-4">
      <h3 class="text-lg font-semibold mb-2">{{ confirmDialog.title }}</h3>
      <p class="text-sm text-gray-600 mb-4">{{ confirmDialog.message }}</p>
      <p v-if="confirmDialog.error" class="text-sm text-red-600 mt-2">{{ confirmDialog.error }}</p>
      <div class="flex justify-end gap-3 mt-4">
        <button @click="confirmDialog.show = false" :disabled="confirmDialog.loading" class="px-4 py-2 border rounded-lg text-sm disabled:opacity-50">取消</button>
        <button @click="handleConfirm" :disabled="confirmDialog.loading" :class="confirmDialog.btnClass" class="px-4 py-2 rounded-lg text-sm text-white disabled:opacity-50">
          <span v-if="confirmDialog.loading">處理中...</span>
          <span v-else>確認</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useMetaStore } from '../stores/meta'
import { useAuthStore } from '../stores/auth'
import api from '../utils/api'

const route = useRoute()
const router = useRouter()
const meta = useMetaStore()
const auth = useAuthStore()

const caseData = ref(null)
const activeTab = ref('info')

const showLogForm = ref(false)
const showEstForm = ref(false)
const showReplyForm = ref(false)

const logForm = ref({ log_date: new Date().toISOString().slice(0, 10), handling_method: '', handling_result: '', hours_spent: 0 })

const assignModal = ref({
  show: false,
  search: '',
  selectedIds: [],
  instructions: '',
  expectedDate: '',
  submitting: false,
  error: ''
})
const estForm = ref({ request_date: new Date().toISOString().slice(0, 10), summary: '', estimated_hours: 0, estimator_user_id: null, remarks: '' })
const replyForm = ref({ reply_date: new Date().toISOString().slice(0, 10), reply_content: '' })
const confirmDialog = ref({ show: false, title: '', message: '', onConfirm: () => {}, btnClass: 'bg-indigo-600', loading: false, error: '' })

const caseId = computed(() => route.params.id)

const tabs = computed(() => [
  { key: 'info', label: '基本資訊' },
  { key: 'logs', label: '處理歷程', count: caseData.value?.logs?.length || 0 },
  { key: 'assign', label: '派工', count: activeAssignments.value.length },
  { key: 'estimation', label: '工時評估', count: caseData.value?.estimations?.length || 0 },
  { key: 'replies', label: '客戶回覆', count: caseData.value?.replies?.length || 0 }
])

const activeAssignments = computed(() => (caseData.value?.assignments || []).filter(a => a.is_active))
const inactiveAssignments = computed(() => (caseData.value?.assignments || []).filter(a => !a.is_active))
const activeAssignmentIds = computed(() => activeAssignments.value.map(a => a.se?.id).filter(Boolean))

const caseAttachments = computed(() => (caseData.value?.attachments || []).filter(a => a.entity_type === 'case'))

const availableSEs = computed(() => {
  if (!caseData.value) return []
  const byCacheProject = meta.getProjectSEs(caseData.value.project?.id)
  return byCacheProject.length ? byCacheProject : meta.users.filter(u => u.role === 'SE')
})

const filteredAssignSEs = computed(() => {
  const q = assignModal.value.search.trim().toLowerCase()
  return availableSEs.value.filter(se => !q || se.full_name.toLowerCase().includes(q))
})

function seNameById(id) {
  return availableSEs.value.find(s => s.id === id)?.full_name || String(id)
}

function openAssignModal() {
  assignModal.value = {
    show: true,
    search: '',
    selectedIds: [...activeAssignmentIds.value],
    instructions: '',
    expectedDate: '',
    submitting: false,
    error: ''
  }
}

const slaBadge = computed(() => {
  if (!caseData.value?.due_at) return null
  const ms = new Date(caseData.value.due_at) - Date.now()
  if (ms < 0) return '已逆期'
  const hrs = Math.floor(ms / 3600000)
  if (hrs < 24) return `剩 ${hrs}h`
  const days = Math.floor(hrs / 24)
  return `剩 ${days}d`
})

const slaUrgent = computed(() => {
  if (!caseData.value?.due_at) return false
  const ms = new Date(caseData.value.due_at) - Date.now()
  return ms < 0 || ms < 24 * 3600000
})

const availableActions = computed(() => {
  if (!caseData.value) return []
  const s = caseData.value.status
  const r = auth.role
  const actions = []

  // 新增處理紀錄: [10,20,30,35], PM/SE/SysAdmin — status 30 時降格為 outline
  if ([10, 20, 30, 35].includes(s) && ['PM', 'SE', 'SysAdmin'].includes(r))
    actions.push({
      label: '新增處理紀錄',
      handler: () => { activeTab.value = 'logs'; showLogForm.value = true },
      class: s === 30
        ? 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
        : 'bg-brand-700 text-white hover:bg-brand-800'
    })

  // 轉派 SE: [10,35], PM/SysAdmin
  if ([10, 35].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '轉派 SE', handler: () => { activeTab.value = 'assign'; openAssignModal() }, class: 'bg-blue-600 text-white hover:bg-blue-700' })

  // 回報完工: [30], SE/SysAdmin — 綠色 + pulse
  if (s === 30 && ['SE', 'SysAdmin'].includes(r))
    actions.push({ label: '回報完工', handler: () => doAction('complete', '確認此案件已完工？'), class: 'bg-green-600 text-white hover:bg-green-700 animate-pulse' })

  // 回覆客戶: [10,30], PM/SysAdmin
  if ([10, 30].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '回覆客戶', handler: () => { activeTab.value = 'replies'; showReplyForm.value = true }, class: 'bg-teal-600 text-white hover:bg-teal-700' })

  // 確認結案 + 退回: [40], PM/SysAdmin
  if (s === 40 && ['PM', 'SysAdmin'].includes(r)) {
    actions.push({ label: '確認結案', handler: () => doAction('close', '確認結案？'), class: 'bg-emerald-600 text-white hover:bg-emerald-700' })
    actions.push({ label: '退回', handler: () => doAction('return', '確認退回此案件？'), class: 'bg-red-500 text-white hover:bg-red-600' })
  }

  // 取消: [10,20,30,35,40], PM/SysAdmin
  if ([10, 20, 30, 35, 40].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '取消', handler: () => doAction('cancel', '確認取消此案件？此操作不可逆'), class: 'bg-gray-600 text-white hover:bg-gray-700' })

  // 重開: [50,60], PM/SysAdmin
  if ([50, 60].includes(s) && ['PM', 'SysAdmin'].includes(r))
    actions.push({ label: '重開', handler: () => doAction('reopen', '將以舊案為範本建立新案件'), class: 'bg-indigo-600 text-white hover:bg-indigo-700' })

  return actions
})

async function fetchCase() {
  const { data: res } = await api.get(`/cases/${caseId.value}`)
  if (res.success) caseData.value = res.data
}

function doAction(action, message) {
  confirmDialog.value = {
    show: true,
    title: '確認操作',
    message,
    btnClass: action === 'cancel' ? 'bg-red-600' : 'bg-indigo-600',
    loading: false,
    error: '',
    onConfirm: async () => {
      if (action === 'reopen') {
        const { data: res } = await api.post(`/cases/${caseId.value}/${action}`)
        if (res.success) router.push(`/cases/${res.data.id}`)
      } else {
        await api.post(`/cases/${caseId.value}/${action}`)
        await fetchCase()
      }
    }
  }
}

async function handleConfirm() {
  confirmDialog.value.loading = true
  confirmDialog.value.error = ''
  try {
    await confirmDialog.value.onConfirm()
    confirmDialog.value.show = false
  } catch (err) {
    confirmDialog.value.error = err?.response?.data?.error?.message || err?.message || '操作失敗，請稍後再試'
  } finally {
    confirmDialog.value.loading = false
  }
}

async function submitLog() {
  await api.post(`/cases/${caseId.value}/logs`, logForm.value)
  showLogForm.value = false
  logForm.value = { log_date: new Date().toISOString().slice(0, 10), handling_method: '', handling_result: '', hours_spent: 0 }
  await fetchCase()
}

async function submitAssign() {
  if (assignModal.value.selectedIds.length === 0) {
    assignModal.value.error = '請至少選擇一位 SE 工程師'
    return
  }
  assignModal.value.error = ''
  assignModal.value.submitting = true
  try {
    const payload = {
      se_user_ids: assignModal.value.selectedIds,
      primary_se_user_id: assignModal.value.selectedIds[0],
      instructions: assignModal.value.instructions || null,
      expected_completion_date: assignModal.value.expectedDate || null
    }
    const { data: res } = await api.post(`/cases/${caseId.value}/assign`, payload)
    if (!res.success) throw new Error(res.error?.message || '派工失敗')
    assignModal.value.show = false
    await fetchCase()
  } catch (err) {
    assignModal.value.error = err?.response?.data?.error?.message || err?.message || '派工失敗，請稍後再試'
  } finally {
    assignModal.value.submitting = false
  }
}

async function submitEstimation() {
  await api.post(`/cases/${caseId.value}/estimations`, estForm.value)
  showEstForm.value = false
  estForm.value = { request_date: new Date().toISOString().slice(0, 10), summary: '', estimated_hours: 0, estimator_user_id: null, remarks: '' }
  await fetchCase()
}

async function submitReply() {
  await api.post(`/cases/${caseId.value}/replies`, replyForm.value)
  showReplyForm.value = false
  replyForm.value = { reply_date: new Date().toISOString().slice(0, 10), reply_content: '' }
  await fetchCase()
}

function formatFileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function estStatusLabel(s) { return { 10: '待評估', 20: '評估中', 30: '已回覆' }[s] || s }
function estStatusColor(s) { return { 10: 'bg-gray-100 text-gray-800', 20: 'bg-yellow-100 text-yellow-800', 30: 'bg-green-100 text-green-800' }[s] }

onMounted(fetchCase)
</script>

<style scoped>
@reference "tailwindcss";
.label { @apply block text-sm font-medium text-gray-700 mb-1 }
.input-base { @apply w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 }
.badge { @apply inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium }
.th-cell { @apply px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap }
.td-cell { @apply px-4 py-3 text-sm whitespace-nowrap }
</style>
