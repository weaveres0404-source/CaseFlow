<template>
  <div class="mx-auto w-full max-w-[980px] px-4 py-6 pb-24 md:px-6 lg:px-7">
    <div class="w-full">
    <div class="mb-6 flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
      <div class="min-w-0">
        <div class="mb-1 inline-grid grid-flow-col auto-cols-max items-center gap-1 text-xs text-slate-500">
          <router-link to="/dashboard" class="hover:text-slate-700">儀表板</router-link>
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <router-link to="/cases" class="hover:text-slate-700">案件列表</router-link>
          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
          <span class="text-slate-700">新增案件</span>
        </div>
        <div class="grid grid-flow-col auto-cols-max items-center gap-3">
          <h1 class="text-xl md:text-[22px] font-bold text-slate-900 tracking-tight">新增案件</h1>
        </div>
      </div>
      <div class="shrink-0 md:self-start">
        <button type="button" @click="openSimilarModal"
          class="inline-grid h-9 grid-flow-col auto-cols-max items-center gap-1.5 rounded-lg border border-indigo-200 bg-indigo-50 px-3 text-sm font-medium text-indigo-800 hover:bg-indigo-100">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
          從歷史案件引用
        </button>
      </div>
    </div>

    <div v-if="citedFrom" class="mb-4 grid grid-cols-[auto_1fr_auto] items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 p-3">
      <svg class="w-4 h-4 text-amber-700 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-4l-4 4v-4z"/></svg>
      <div class="min-w-0 text-[13px] text-amber-900">已引用 <b class="tabular-nums">{{ citedFrom }}</b> 的欄位，請視需要修改後再送出。<span class="text-amber-700">（不會建立案件關聯）</span></div>
      <button type="button" @click="clearCite" class="inline-grid grid-flow-col auto-cols-max items-center gap-1 text-xs text-amber-700 hover:text-amber-900">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
        清除
      </button>
    </div>

    <form @submit.prevent="submit" class="flex flex-col gap-6">
      <section class="card-section">
        <div class="card-head">
          <div class="card-head-num">1</div>
          <h2 class="text-[15px] font-semibold text-slate-900">基本資訊</h2>
        </div>
        <div class="card-body">
          <div class="grid grid-cols-1 gap-x-6 gap-y-5 md:grid-cols-2">
            <div>
              <label class="field-label">專案 <span class="req">*</span></label>
              <select v-model="form.project_id" class="input-base" required @change="onProjectChange">
                <option :value="null">請選擇…</option>
                <option v-for="project in availableProjects" :key="project.id" :value="project.id">{{ project.code }} {{ project.name }}</option>
              </select>
            </div>
            <div>
              <label class="field-label">客戶</label>
              <div class="input-base flex items-center bg-slate-50 text-slate-600 cursor-default select-none">
                {{ selectedCustomerName || '（請先選擇專案）' }}
              </div>
            </div>

            <div>
              <label class="field-label">報修人 <span class="req">*</span></label>
              <input v-model.trim="form.reporter_name" type="text" class="input-base" placeholder="客戶端聯絡人姓名" required />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="field-label">聯絡電話</label>
                <input v-model.trim="form.reporter_phone" type="text" class="input-base tabular-nums" placeholder="02-xxxxxxxx / 09xx" />
              </div>
              <div>
                <label class="field-label">Email</label>
                <input v-model.trim="form.reporter_email" type="email" class="input-base" placeholder="contact@example.com" />
              </div>
            </div>

            <div class="md:col-span-2">
              <label class="field-label">案件類型 <span class="req">*</span></label>
              <div class="flex flex-wrap gap-2">
                <button v-for="type in caseTypes" :key="type.value" type="button" class="seg-btn" :class="{ active: form.case_type === type.value }" @click="onCaseTypeChange(type.value)">
                  <span class="seg-dot"></span>{{ type.label }}
                </button>
              </div>
            </div>

            <div>
              <label class="field-label">問題分類 <span class="req">*</span></label>
              <select v-model="form.category_id" class="input-base" required>
                <option :value="null">請選擇…</option>
                <option v-for="category in filteredCategories" :key="category.id" :value="category.id">{{ category.name }}</option>
              </select>
            </div>

            <div>
              <label class="field-label">系統 / 模組 <span class="text-slate-400 text-[10px] font-normal">（依所選專案自動載入）</span></label>
              <select v-model="form.module_id" class="input-base">
                <option :value="null">請選擇…</option>
                <option v-for="module in filteredModules" :key="module.id" :value="module.id">{{ module.name }}</option>
              </select>
            </div>

            <div class="md:col-span-2">
              <label class="field-label">處理截止時間 <span class="req">*</span> <span class="text-slate-400 text-[10px] font-normal">（SLA 倒數與「剩 Xh」依此計算）</span></label>
              <div class="flex flex-wrap items-center gap-2">
                <input v-model="form.due_at" type="datetime-local" class="input-base !w-auto min-w-[220px]" />
                <div class="flex flex-wrap items-center gap-2">
                  <button v-for="item in quickDates" :key="item.label" type="button" class="seg-btn text-[12px]" @click="addDueHours(item.h)">{{ item.label }}</button>
                </div>
                <span class="ml-1 text-[11px]" :class="dueHint.className">{{ dueHint.text }}</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="card-section">
        <div class="card-head">
          <div class="card-head-num">2</div>
          <h2 class="text-[15px] font-semibold text-slate-900">{{ sec2Title }}</h2>
          <span class="text-[11px] text-slate-400 ml-auto">{{ sec2Hint }}</span>
        </div>
        <div class="card-body">
          <label class="field-label">詳細描述 <span class="req">*</span></label>
          <textarea v-model="form.description" rows="8" class="input-base resize-y min-h-[220px]" :placeholder="sec2Placeholder" required></textarea>
          <div class="mt-2 grid grid-cols-[1fr_auto] items-center gap-3">
            <div class="text-[11px] text-slate-400">最多 {{ maxDescriptionLength }} 字</div>
            <div class="text-[11px] tabular-nums" :class="descriptionCountClass">{{ form.description.length }} / {{ maxDescriptionLength }}</div>
          </div>
        </div>
      </section>

      <section class="card-section">
        <div class="card-head">
          <div class="card-head-num">3</div>
          <h2 class="text-[15px] font-semibold text-slate-900">附件</h2>
          <span class="text-[11px] text-slate-400 ml-auto">單檔 ≤ 20MB · 支援圖片 / 文件 / 壓縮檔</span>
        </div>
        <div class="card-body">
          <input ref="fileInput" type="file" class="hidden" multiple @change="handleFileChange" />
          <div class="drop-zone" :class="{ 'drop-zone--active': isDragActive }" @click="openFilePicker" @dragenter.prevent="isDragActive = true" @dragover.prevent="isDragActive = true" @dragleave.prevent="isDragActive = false" @drop.prevent="handleDrop">
            <div class="grid justify-items-center gap-2">
              <div class="w-10 h-10 rounded-full bg-indigo-50 grid place-items-center">
                <svg class="w-5 h-5 text-indigo-700" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/></svg>
              </div>
              <div class="text-sm text-slate-700 font-medium">拖放檔案至此，或<button type="button" class="text-indigo-700 hover:underline mx-1">選擇檔案</button></div>
              <div class="text-[11px] text-slate-500">單次可上傳多檔；PNG / JPG / PDF / DOCX / XLSX / ZIP</div>
            </div>
          </div>

          <div v-if="attachments.length > 0" class="mt-3 space-y-1.5">
            <div v-for="item in attachments" :key="item.localId" class="grid grid-cols-[auto_1fr_auto_auto] items-center gap-3 rounded-lg border border-slate-200 bg-white p-2.5">
              <div class="w-8 h-8 rounded-md grid place-items-center" :class="attachmentIconClass(item)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 2v6h6"/></svg>
              </div>
              <div class="min-w-0">
                <div class="text-[13px] text-slate-800 truncate">{{ item.file_name }}</div>
                <div class="text-[11px] text-slate-500 tabular-nums">{{ formatFileSize(item.file_size) }} · {{ attachmentStatusText(item) }}</div>
                <div v-if="item.status === 'uploading'" class="h-1.5 w-24 bg-slate-100 rounded-full overflow-hidden mt-1">
                  <div class="h-full bg-indigo-500" :style="{ width: `${item.progress}%` }"></div>
                </div>
              </div>
              <span v-if="item.status === 'uploaded'" class="inline-grid grid-flow-col auto-cols-max items-center gap-1 text-[11px] text-emerald-700">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                已上傳
              </span>
              <button type="button" class="text-slate-400 hover:text-rose-600 p-1" @click="removeAttachment(item.localId)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>
          </div>
        </div>
      </section>

      <div class="sticky bottom-0 -mx-4 bg-gradient-to-t from-slate-50 via-slate-50 to-slate-50/0 px-4 py-4 md:-mx-6 md:px-6 lg:-mx-7 lg:px-7">
        <div class="card-section flex items-center gap-3 px-4 py-3">

          <div class="flex-1"></div>
          <div class="grid gap-2 sm:grid-flow-col sm:auto-cols-max sm:justify-end">
            <router-link to="/cases" class="inline-grid h-9 grid-flow-col auto-cols-max items-center gap-1.5 rounded-lg px-4 text-sm text-slate-600 hover:bg-slate-100">取消</router-link>
            <button type="button" @click="saveDraft" class="inline-grid h-9 grid-flow-col auto-cols-max items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3.5 text-sm text-slate-700 hover:bg-slate-50">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
              儲存草稿
            </button>
            <button type="submit" :disabled="!canSubmit || submitting" class="inline-grid h-9 grid-flow-col auto-cols-max items-center gap-1.5 rounded-lg bg-indigo-700 px-4 text-sm font-medium text-white shadow-sm hover:bg-indigo-800 disabled:cursor-not-allowed disabled:bg-slate-300">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h14M12 5l7 7-7 7"/></svg>
              {{ submitting ? '建立中…' : '建立案件' }}
            </button>
          </div>
        </div>
      </div>
    </form>



    <div v-if="showSimilarModal" class="fixed inset-0 z-50 grid place-items-center bg-slate-950/45 p-4" @click.self="closeSimilarModal">
      <div class="modal-panel">
        <div class="grid grid-cols-[auto_1fr_auto] items-center gap-2 border-b border-slate-200 px-5 py-4">
          <div class="w-8 h-8 rounded-lg bg-indigo-50 text-indigo-700 grid place-items-center">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
          </div>
          <div>
            <div class="text-[15px] font-semibold text-slate-900">從歷史案件引用</div>
            <div class="text-[11px] text-slate-500 mt-0.5">引用會複製欄位到表單，<b>不會</b>在案件間建立關聯</div>
          </div>
          <button type="button" @click="closeSimilarModal" class="p-1.5 hover:bg-slate-100 rounded-md text-slate-500">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>

        <div class="px-5 pt-3.5">
          <div class="relative">
            <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z"/></svg>
            <input v-model="similarQuery" type="text" placeholder="搜尋案件編號 / 報修人 / 問題描述…" class="w-full h-10 pl-9 pr-3 rounded-lg border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent" @keyup.enter="searchSimilar" />
          </div>
          <div class="mt-2 grid gap-2 text-[11px] xl:grid-cols-[auto_auto_auto_auto_1fr_auto_auto] xl:items-center">
            <span class="text-slate-500">篩選：</span>
            <button type="button" class="seg-btn" :class="{ active: citeFilters.closed }" @click="citeFilters.closed = !citeFilters.closed">已結案</button>
            <button type="button" class="seg-btn" :class="{ active: citeFilters.mine }" @click="citeFilters.mine = !citeFilters.mine">我的案件</button>
            <button type="button" class="seg-btn" :class="{ active: citeFilters.sameType }" @click="citeFilters.sameType = !citeFilters.sameType">同分類</button>

            <div class="relative inline-grid items-center">
              <button type="button" class="seg-btn inline-grid grid-flow-col auto-cols-max items-center gap-1 pr-2" @click="dateMenuOpen = !dateMenuOpen">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10m2 8H5a2 2 0 01-2-2V7a2 2 0 012-2h14a2 2 0 012 2v10a2 2 0 01-2 2z"/></svg>
                <span>{{ dateRangeLabel }}</span>
                <svg class="w-3 h-3 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
              </button>
              <div v-if="dateMenuOpen" class="absolute left-0 top-full mt-1 w-[240px] rounded-lg border border-slate-200 bg-white shadow-lg z-10 p-1.5">
                <button v-for="item in dateRanges" :key="item.value" type="button" class="cite-range-btn" :class="{ active: citeFilters.range === item.value }" @click="selectRange(item.value)">{{ item.label }}</button>
                <div class="border-t border-slate-100 my-1"></div>
                <div class="px-2 py-1.5 text-slate-500 text-[11px]">自訂區間</div>
                <div class="grid grid-cols-[1fr_auto_1fr] items-center gap-1 px-2 pb-2">
                  <input v-model="citeFilters.date_from" type="date" class="min-w-0 h-7 px-1.5 text-[11px] rounded border border-slate-200" />
                  <span class="text-slate-400 text-[10px]">～</span>
                  <input v-model="citeFilters.date_to" type="date" class="min-w-0 h-7 px-1.5 text-[11px] rounded border border-slate-200" />
                </div>
                <button type="button" class="mx-2 mb-1 w-[calc(100%-16px)] h-7 rounded bg-slate-900 text-white text-[11px]" @click="applyCustomDateRange">套用自訂區間</button>
              </div>
            </div>

            <span class="justify-self-start xl:justify-self-end text-slate-400 tabular-nums">{{ similarCases.length }} 筆結果</span>
            <button type="button" class="h-8 rounded-lg bg-indigo-700 px-3 text-xs text-white hover:bg-indigo-800 xl:justify-self-end" @click="searchSimilar">搜尋</button>
          </div>
        </div>

        <div class="overflow-y-auto px-5 py-2.5 space-y-2">
          <label v-for="item in similarCases" :key="item.id" class="block rounded-xl border p-2.5 cursor-pointer transition cite-row" :class="selectedSimilarCaseId === item.id ? 'border-indigo-500 bg-indigo-50/60' : 'border-slate-200 hover:border-indigo-300 hover:bg-indigo-50/30'">
            <div class="grid grid-cols-[auto_1fr] items-start gap-3">
              <input v-model="selectedSimilarCaseId" :value="item.id" type="radio" name="citeCase" class="mt-1 w-4 h-4 text-indigo-600" />
              <div class="min-w-0">
                <div class="grid gap-2 sm:grid-flow-col sm:auto-cols-max sm:items-center">
                  <span class="tabular-nums text-[13px] font-semibold text-indigo-800">{{ item.case_number }}</span>
                  <span class="text-[10px] px-1.5 py-0.5 rounded bg-slate-100 text-slate-600">{{ item.customer_short_name }}</span>
                  <span class="pill" :class="caseTypePillClass(item.case_type)">{{ caseTypeLabel(item.case_type) }}</span>
                  <span class="pill" :class="statusPillClass(item.status)">{{ statusLabel(item.status) }}</span>
                  <span class="text-[10px] text-slate-400 tabular-nums">{{ formatDate(item.updated_at) }}</span>
                </div>
                <div class="text-[13px] text-slate-800 mt-1 font-medium">{{ item.title }}</div>
                <div class="text-[12px] text-slate-500 mt-1 line-clamp-2">{{ item.description }}</div>
                <div class="mt-2 grid gap-2 text-[11px] text-slate-500 sm:grid-flow-col sm:auto-cols-max">
                  <span class="inline-grid grid-flow-col auto-cols-max items-center gap-1"><svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.121 17.804A9 9 0 1118.36 4.57M15 11a3 3 0 11-6 0 3 3 0 016 0zm6 10H3"/></svg>立案：{{ item.created_by?.full_name || '—' }}</span>
                  <span class="inline-grid grid-flow-col auto-cols-max items-center gap-1"><svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V7a2 2 0 00-2-2h-3V3H9v2H6a2 2 0 00-2 2v6m16 0l-2.586 2.586A2 2 0 0116 16.172V21H8v-4.828a2 2 0 01-.586-1.414L4 13"/></svg>SE：{{ similarCaseSeLabel(item) }}</span>
                </div>
              </div>
            </div>
          </label>
          <div v-if="similarCases.length === 0" class="py-10 text-center text-slate-400 text-sm">{{ searchDone ? '找不到符合條件的案件' : '輸入條件後按搜尋' }}</div>
        </div>

        <div class="grid gap-3 border-t border-slate-200 bg-slate-50 px-5 py-3 lg:grid-cols-[auto_1fr_auto_auto_auto] lg:items-center">
          <div class="text-[11px] text-slate-500">選擇後可選擇引用方式：</div>
          <button type="button" @click="closeSimilarModal" class="h-9 px-3 rounded-lg text-slate-600 hover:bg-white text-sm">取消</button>
          <button type="button" @click="applySelectedSimilar('desc')" class="inline-grid h-9 grid-flow-col auto-cols-max items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3.5 text-sm text-slate-700 hover:bg-slate-50" title="只把來源案件的詳細描述拼到本案描述欄">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h10"/></svg>
            只引用詳細描述
          </button>
          <button type="button" @click="applySelectedSimilar('full')" class="inline-grid h-9 grid-flow-col auto-cols-max items-center gap-1.5 rounded-lg bg-indigo-700 px-3.5 text-sm font-medium text-white hover:bg-indigo-800">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg>
            整案欄位引用
          </button>
        </div>
      </div>
    </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useMetaStore } from '../stores/meta'
import { useAuthStore } from '../stores/auth'
import api from '../utils/api'

const router = useRouter()
const meta = useMetaStore()
const auth = useAuthStore()

const draftStorageKey = 'caseflow-case-create-draft'
const maxDescriptionLength = 5000

const fileInput = ref(null)
const attachments = ref([])
const isDragActive = ref(false)
const submitting = ref(false)
const citedFrom = ref(null)

const showSimilarModal = ref(false)
const similarQuery = ref('')
const similarCases = ref([])
const selectedSimilarCaseId = ref(null)
const searchDone = ref(false)
const dateMenuOpen = ref(false)
const citeFilters = ref({
  closed: true,
  mine: false,
  sameType: false,
  range: '90',
  date_from: '',
  date_to: ''
})

const VALID_CASE_TYPES = ['REPAIR', 'EVALUATION', 'MAINTENANCE', 'UHD', 'INQUIRY']

const caseTypes = [
  { value: 'REPAIR',      label: '障礙調查' },
  { value: 'EVALUATION',  label: '工時評估' },
  { value: 'MAINTENANCE', label: '日常維運' },
  { value: 'UHD',         label: 'UHD協助' }
]



const sec2Map = {
  REPAIR:      { title: '障礙描述',   hint: '描述清楚可讓 SE 更快排查',           placeholder: '請描述發生的障礙、發生頻率、預期行為、實際行為…' },
  EVALUATION:  { title: '評估需求',   hint: '說明評估範圍與目標，供 SE 回報工時', placeholder: '請描述要評估的範圍、預期產出、交付期限…' },
  MAINTENANCE: { title: '維運內容',   hint: '詳述需要執行的日常維運項目',  placeholder: '請描述維運作業內容、影響範圍、預期完成時間…' },
  UHD:         { title: 'UHD 申請內容', hint: 'UHD 協助作業說明',                    placeholder: '請描述需要協助的作業內容、相關帳號或資料範圍…' },
  INQUIRY:     { title: '詢問內容',   hint: '查詢操作方式或資料確認等',       placeholder: '請描述要詢問的問題或希望協助確認的內容…' }
}

const dateRanges = [
  { value: '7', label: '近 7 天' },
  { value: '30', label: '近 30 天' },
  { value: '90', label: '近 90 天' },
  { value: '180', label: '近半年' },
  { value: '365', label: '近 1 年' },
  { value: 'all', label: '全部時間' }
]

const quickDates = [
  { label: '+4h', h: 4 },
  { label: '+1 天', h: 24 },
  { label: '+3 天', h: 72 },
  { label: '+1 週', h: 168 }
]

const form = ref({
  project_id: null,
  customer_id: null,
  reporter_name: '',
  reporter_phone: '',
  reporter_email: '',
  case_type: 'REPAIR',
  category_id: null,
  module_id: null,
  due_at: '',
  description: ''
})

const availableProjects = computed(() => {
  if (auth.role === 'SysAdmin') return meta.projects
  const userId = auth.user?.user_id
  const myProjectIds = new Set(
    meta.projectMembers.filter(pm => pm.user_id === userId).map(pm => pm.project_id)
  )
  return meta.projects.filter(p => myProjectIds.has(p.id))
})

const filteredModules = computed(() => form.value.project_id ? meta.getModulesByProject(form.value.project_id) : [])
const filteredCategories = computed(() => {
  const type = form.value.case_type
  if (!type) return meta.categories
  return meta.categories.filter(c => !c.case_type_filter || c.case_type_filter === type)
})
const selectedCustomerName = computed(() => {
  if (!form.value.project_id) return ''
  const project = meta.projects.find(p => p.id === form.value.project_id)
  if (!project?.customer_id) return ''
  return meta.customers.find(c => c.id === project.customer_id)?.name ?? ''
})
const sec2Title = computed(() => sec2Map[form.value.case_type]?.title || sec2Map.REPAIR.title)
const sec2Hint = computed(() => sec2Map[form.value.case_type]?.hint || sec2Map.REPAIR.hint)
const sec2Placeholder = computed(() => sec2Map[form.value.case_type]?.placeholder || sec2Map.REPAIR.placeholder)
const descriptionCountClass = computed(() => form.value.description.length > maxDescriptionLength ? 'text-rose-500' : 'text-slate-400')
const canSubmit = computed(() => {
  return Boolean(
    form.value.project_id &&
    form.value.customer_id &&
    form.value.reporter_name &&
    form.value.category_id &&
    form.value.case_type &&
    form.value.due_at &&
    form.value.description &&
    form.value.description.length <= maxDescriptionLength
  )
})

const dueHint = computed(() => {
  if (!form.value.due_at) return { text: '', className: 'text-slate-500' }
  const diffHours = (new Date(form.value.due_at) - new Date()) / 36e5
  if (diffHours < 0) return { text: `已逾時 ${Math.abs(diffHours).toFixed(1)}h`, className: 'text-rose-600 font-medium' }
  if (diffHours < 8) return { text: `剩 ${diffHours.toFixed(1)}h（SLA 緊迫）`, className: 'text-rose-600 font-medium' }
  if (diffHours < 48) return { text: `剩 ${Math.round(diffHours)}h（約 ${(diffHours / 24).toFixed(1)} 天）`, className: 'text-amber-600 font-medium' }
  return { text: `剩 ${Math.round(diffHours / 24)} 天`, className: 'text-slate-600' }
})

const dateRangeLabel = computed(() => {
  if (citeFilters.value.date_from && citeFilters.value.date_to) {
    return `${citeFilters.value.date_from} ~ ${citeFilters.value.date_to}`
  }
  return dateRanges.find(item => item.value === citeFilters.value.range)?.label || '近 90 天'
})

function pad(number) {
  return String(number).padStart(2, '0')
}

function toLocalInput(date) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function formatFileSize(bytes) {
  if (!bytes) return '0 KB'
  if (bytes >= 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`
  return `${Math.round(bytes / 1024)} KB`
}

function formatDate(value) {
  if (!value) return '—'
  return new Date(value).toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit' })
}

function caseTypeLabel(value) {
  return meta.caseTypeMap[value]?.label || value || '未分類'
}

function caseTypePillClass(value) {
  return meta.caseTypeMap[value]?.color || 'bg-slate-100 text-slate-600 ring-1 ring-slate-200'
}

function statusLabel(status) {
  return meta.statusMap[status]?.label || String(status)
}

function statusPillClass(status) {
  return meta.statusMap[status]?.color || 'bg-slate-100 text-slate-600 ring-1 ring-slate-200'
}

function similarCaseSeLabel(item) {
  if (item.assigned_ses?.length) {
    return item.assigned_ses.map(se => se.full_name).join('、')
  }
  return '—'
}

function onCaseTypeChange(type) {
  form.value.case_type = type
  form.value.category_id = null
}

function onProjectChange() {
  form.value.module_id = null
  const project = meta.projects.find(item => item.id === form.value.project_id)
  if (project?.customer_id) {
    form.value.customer_id = project.customer_id
  }
}

function addDueHours(hours) {
  const base = form.value.due_at ? new Date(form.value.due_at) : new Date()
  base.setHours(base.getHours() + hours)
  form.value.due_at = toLocalInput(base)
}

function attachmentIconClass(item) {
  if (item.status === 'uploaded') return 'bg-emerald-50 text-emerald-700'
  if (item.status === 'error') return 'bg-rose-50 text-rose-700'
  if (item.file_name.match(/\.(png|jpg|jpeg|gif|webp)$/i)) return 'bg-rose-50 text-rose-700'
  return 'bg-slate-100 text-slate-600'
}

function attachmentStatusText(item) {
  if (item.status === 'uploading') return `上傳中 · ${item.progress}%`
  if (item.status === 'uploaded') return '剛上傳'
  if (item.status === 'error') return item.error || '上傳失敗'
  return '待建立案件後一併上傳'
}

function openFilePicker() {
  fileInput.value?.click()
}

function makeAttachment(file) {
  return {
    localId: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
    file,
    file_name: file.name,
    file_size: file.size,
    progress: 0,
    status: 'pending',
    error: ''
  }
}

function addFiles(fileList) {
  Array.from(fileList || []).forEach(file => {
    if (attachments.value.some(item => item.file_name === file.name && item.file_size === file.size)) return
    if (file.size > 20 * 1024 * 1024) {
      attachments.value.push({ ...makeAttachment(file), status: 'error', error: '檔案超過 20MB' })
      return
    }
    attachments.value.push(makeAttachment(file))
  })
}

function handleFileChange(event) {
  addFiles(event.target.files)
  event.target.value = ''
}

function handleDrop(event) {
  isDragActive.value = false
  addFiles(event.dataTransfer?.files)
}

function removeAttachment(localId) {
  attachments.value = attachments.value.filter(item => item.localId !== localId)
}

function saveDraft() {
  localStorage.setItem(draftStorageKey, JSON.stringify({ form: form.value, citedFrom: citedFrom.value }))
  window.alert('草稿已儲存')
}

function loadDraft() {
  const raw = localStorage.getItem(draftStorageKey)
  if (!raw) return
  try {
    const draft = JSON.parse(raw)
    if (draft?.form) {
      const merged = { ...form.value, ...draft.form }
      // 若草稿 case_type 已不在允許清單，重設為預設值
      if (!VALID_CASE_TYPES.includes(merged.case_type)) {
        merged.case_type = 'REPAIR'
      }
      form.value = merged
    }
    if (draft?.citedFrom) {
      citedFrom.value = draft.citedFrom
    }
  } catch {
    localStorage.removeItem(draftStorageKey)
  }
}

function clearDraft() {
  localStorage.removeItem(draftStorageKey)
}

function clearCite() {
  citedFrom.value = null
}

function selectRange(value) {
  citeFilters.value.range = value
  citeFilters.value.date_from = ''
  citeFilters.value.date_to = ''
  dateMenuOpen.value = false
}

function applyCustomDateRange() {
  if (!citeFilters.value.date_from || !citeFilters.value.date_to) {
    window.alert('請同時選擇起訖日期')
    return
  }
  citeFilters.value.range = 'custom'
  dateMenuOpen.value = false
}

function buildSimilarSearchParams() {
  const params = { page_size: 10 }
  if (similarQuery.value.trim()) params.q = similarQuery.value.trim()
  if (citeFilters.value.closed) params.status = 50
  if (citeFilters.value.mine) params.created_by_me = true
  if (citeFilters.value.sameType) params.case_type = form.value.case_type

  if (citeFilters.value.date_from && citeFilters.value.date_to) {
    params.date_from = citeFilters.value.date_from
    params.date_to = citeFilters.value.date_to
    return params
  }

  if (citeFilters.value.range !== 'all' && citeFilters.value.range !== 'custom') {
    const rangeDays = Number(citeFilters.value.range)
    const start = new Date()
    start.setDate(start.getDate() - rangeDays)
    params.date_from = [start.getFullYear(), String(start.getMonth()+1).padStart(2,'0'), String(start.getDate()).padStart(2,'0')].join('-')
  }

  return params
}

async function searchSimilar() {
  try {
    const { data: res } = await api.get('/cases', { params: buildSimilarSearchParams() })
    if (res.success) {
      similarCases.value = res.data
      selectedSimilarCaseId.value = res.data[0]?.id || null
    }
    searchDone.value = true
  } catch {
    similarCases.value = []
    searchDone.value = true
  }
}

function openSimilarModal() {
  showSimilarModal.value = true
  if (!searchDone.value) {
    searchSimilar()
  }
}

function closeSimilarModal() {
  showSimilarModal.value = false
  dateMenuOpen.value = false
}

function appendReferencedDescription(sourceCase) {
  const prefix = `【引用自 ${sourceCase.case_number}】${sourceCase.title}`
  const body = [prefix, '', sourceCase.description || '', '', '---', '（以下請補充本次案件的實際差異）', ''].join('\n')
  form.value.description = body
}

async function applySelectedSimilar(mode) {
  const selected = similarCases.value.find(item => item.id === selectedSimilarCaseId.value)
  if (!selected) {
    window.alert('請先選擇要引用的案件')
    return
  }

  if (mode === 'full') {
    // 必須取完整 detail，list API 不含 reporter_name、phone、email、category_id、module_id
    try {
      const { data: res } = await api.get(`/cases/${selected.id}`)
      if (res.success) {
        const detail = res.data
        form.value.project_id  = detail.project?.id   ?? null
        form.value.customer_id = detail.customer?.id  ?? null
        form.value.category_id = detail.category?.id  ?? null
        form.value.module_id   = detail.module?.id    ?? null
        form.value.reporter_name  = detail.reporter_name  || form.value.reporter_name
        form.value.reporter_phone = detail.reporter_phone || ''
        form.value.reporter_email = detail.reporter_email || ''
        form.value.case_type = detail.case_type || form.value.case_type
        appendReferencedDescription(detail)
        citedFrom.value = detail.case_number
      }
    } catch {
      window.alert('引用失敗，請稍後再試')
      return
    }
  } else {
    appendReferencedDescription(selected)
    citedFrom.value = selected.case_number
  }

  closeSimilarModal()
}

async function uploadAttachments(caseId) {
  const failedFiles = []

  for (const item of attachments.value.filter(entry => entry.status !== 'error')) {
    item.status = 'uploading'
    item.progress = 0

    const formData = new FormData()
    formData.append('file', item.file)
    formData.append('entity_type', 'case')
    formData.append('entity_id', String(caseId))

    try {
      await api.post('/attachments', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress(event) {
          if (!event.total) return
          item.progress = Math.round((event.loaded / event.total) * 100)
        }
      })
      item.status = 'uploaded'
      item.progress = 100
    } catch (error) {
      item.status = 'error'
      item.error = error?.response?.data?.error?.message || '附件上傳失敗'
      failedFiles.push(item.file_name)
    }
  }

  return failedFiles
}

async function submit() {
  if (!canSubmit.value) {
    window.alert('請確認必填欄位已完成，且描述未超過字數限制')
    return
  }

  submitting.value = true
  try {
    const payload = {
      ...form.value,
      due_at: form.value.due_at || null
    }
    const { data: res } = await api.post('/cases', payload)
    if (!res.success) {
      throw new Error(res.error?.message || '建立失敗')
    }

    const caseId = res.data.id
    const failedUploads = await uploadAttachments(caseId)

    clearDraft()

    if (failedUploads.length > 0) {
      window.alert(`案件已建立，但以下附件上傳失敗，可在案件詳情補傳：${failedUploads.join('、')}`)
    }

    router.push(`/cases/${caseId}`)
  } catch (error) {
    window.alert(error?.response?.data?.error?.message || error?.message || '建立失敗')
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  if (!meta.loaded) {
    await meta.fetchDropdowns()
  }
  loadDraft()
})
</script>

<style scoped>
@reference "tailwindcss";

.field-label {
  @apply mb-1 inline-grid grid-flow-col auto-cols-max items-center gap-1 text-xs font-medium text-slate-600;
}

.field-label .req {
  @apply text-rose-600 font-semibold;
}

.input-base {
  @apply h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm focus:border-transparent focus:outline-none focus:ring-2 focus:ring-indigo-500;
}

textarea.input-base {
  @apply h-auto py-3 leading-7;
}

.seg-btn {
  @apply inline-grid grid-flow-col auto-cols-max items-center gap-1.5 rounded-[7px] border border-slate-200 bg-white px-3 py-[7px] text-[13px] text-slate-600 transition;
}

.seg-btn:hover {
  @apply border-slate-300 text-slate-900;
}

.seg-btn.active {
  @apply bg-indigo-50 text-indigo-800 border-indigo-200;
}

.seg-btn.active .seg-dot {
  @apply bg-indigo-600;
}

.seg-dot {
  @apply w-1.5 h-1.5 rounded-full bg-slate-300;
}

.drop-zone {
  @apply cursor-pointer rounded-[10px] border-2 border-dashed border-slate-300 bg-slate-50 p-8 text-center transition;
}

.drop-zone:hover,
.drop-zone--active {
  @apply border-indigo-500 bg-indigo-50;
}

.card-section {
  @apply rounded-[14px] border border-slate-200 bg-white;
}

.card-head {
  @apply grid grid-cols-[auto_auto_1fr] items-center gap-3 border-b border-slate-100 px-6 py-5;
}

.card-head-num {
  @apply w-[22px] h-[22px] rounded-full bg-indigo-50 text-indigo-800 text-[11px] font-semibold grid place-items-center tabular-nums;
}

.card-body {
  @apply px-6 py-6;
}

.modal-panel {
  @apply grid max-h-[86vh] w-full max-w-[860px] grid-rows-[auto_auto_1fr_auto] overflow-hidden rounded-[16px] bg-white;
}

.pill {
  @apply inline-grid grid-flow-col auto-cols-max items-center gap-1 rounded-full px-2 py-0.5 text-[11px] ring-1;
}

.cite-range-btn {
  @apply w-full text-left px-2.5 py-1.5 rounded hover:bg-slate-100;
}

.cite-range-btn.active {
  @apply bg-indigo-50 text-indigo-700;
}
</style>
