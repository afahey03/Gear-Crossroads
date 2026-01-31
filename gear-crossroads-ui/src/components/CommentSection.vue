<template>
  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-md dark:shadow-gray-900 p-6">
    <h2 class="text-2xl font-bold mb-6 text-gray-900 dark:text-gray-100">
      Comments ({{ comments.length }})
    </h2>

    <!-- Delete Confirmation Dialog -->
    <ConfirmDialog
      v-model="showDeleteConfirm"
      :message="'Are you sure you want to delete this comment? This action cannot be undone.'"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />

    <!-- Add Comment Form (if logged in) -->
    <div v-if="isLoggedIn" class="mb-6">
      <textarea
        v-model="newComment"
        placeholder="Write a comment..."
        rows="3"
        maxlength="2000"
        class="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-500 dark:placeholder-gray-400"
      ></textarea>
      <div class="flex items-center justify-between mt-2">
        <span class="text-sm text-gray-500 dark:text-gray-400">
          {{ newComment.length }} / 2000
        </span>
        <button
          @click="submitComment"
          :disabled="!newComment.trim() || isSubmitting"
          class="bg-blue-600 dark:bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:cursor-not-allowed font-semibold transition"
        >
          {{ isSubmitting ? 'Posting...' : 'Post Comment' }}
        </button>
      </div>
    </div>

    <!-- Login Prompt -->
    <div v-else class="mb-6 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
      <p class="text-gray-700 dark:text-gray-300">
        <router-link to="/login" class="text-blue-600 dark:text-blue-400 hover:underline font-semibold">
          Log in
        </router-link>
        to join the conversation
      </p>
    </div>

    <!-- Comments List -->
    <div v-if="comments.length > 0" class="space-y-4">
      <div
        v-for="comment in comments"
        :key="comment.id"
        class="border-b border-gray-200 dark:border-gray-700 pb-4 last:border-b-0"
      >
        <!-- Comment Header -->
        <div class="flex items-start justify-between mb-2">
          <div class="flex-1">
            <span class="font-semibold text-gray-900 dark:text-gray-100">
              {{ comment.username }}
            </span>
            <span v-if="comment.isSetupOwner" class="ml-2 text-sm text-blue-600 dark:text-blue-400 font-medium">
              (Poster)
            </span>
          </div>
          <span class="text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap ml-4">
            {{ getFormattedDate(comment.createdAt) }}
          </span>
        </div>

        <!-- Comment Content -->
        <div v-if="editingCommentId === comment.id" class="mt-2">
          <textarea
            v-model="editContent"
            rows="3"
            maxlength="2000"
            class="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
          ></textarea>
          <div class="flex items-center justify-between mt-2">
            <span class="text-sm text-gray-500 dark:text-gray-400">
              {{ editContent.length }} / 2000
            </span>
            <div class="flex gap-2">
              <button
                @click="cancelEdit"
                class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 font-medium"
              >
                Cancel
              </button>
              <button
                @click="saveEdit(comment.id)"
                :disabled="!editContent.trim() || isSubmitting"
                class="bg-blue-600 dark:bg-blue-500 text-white px-6 py-2 rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:cursor-not-allowed font-semibold transition"
              >
                Save
              </button>
            </div>
          </div>
        </div>
        <div v-else>
          <p class="text-gray-700 dark:text-gray-300" :class="{ 'italic text-gray-500 dark:text-gray-500': comment.isDeleted }">
            {{ comment.content }}
            <span v-if="comment.editedAt && !comment.isDeleted" class="text-sm text-gray-500 dark:text-gray-400 italic ml-1">
              (edited)
            </span>
          </p>

          <!-- Comment Actions -->
          <div v-if="!comment.isDeleted" class="flex gap-3 mt-2">
            <button
              v-if="canModifyComment(comment)"
              @click="startEdit(comment)"
              class="text-sm text-blue-600 dark:text-blue-400 hover:underline"
            >
              Edit
            </button>
            <button
              v-if="canDeleteComment(comment)"
              @click="deleteComment(comment.id)"
              class="text-sm text-red-600 dark:text-red-400 hover:underline"
            >
              Delete
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- No Comments -->
    <div v-else class="text-center py-8 text-gray-500 dark:text-gray-400">
      No comments yet. Be the first to comment!
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useUserStore } from '../stores/user'
import { useAlertStore } from '../stores/alert'
import ConfirmDialog from './ConfirmDialog.vue'

interface Comment {
  id: number
  content: string
  createdAt: string
  editedAt: string | null
  isDeleted: boolean
  setupId: number
  userId: string
  username: string
  isSetupOwner: boolean
}

const props = defineProps<{
  setupId: number
}>()

const userStore = useUserStore()
const alertStore = useAlertStore()

const comments = ref<Comment[]>([])
const newComment = ref('')
const editingCommentId = ref<number | null>(null)
const editContent = ref('')
const isSubmitting = ref(false)
const showDeleteConfirm = ref(false)
const commentToDelete = ref<number | null>(null)

const isLoggedIn = computed(() => userStore.isLoggedIn)
const currentUserId = computed(() => userStore.userId)
const isAdmin = computed(() => userStore.isAdmin)

async function loadComments() {
  try {
    const response = await fetch(`https://gearcrossroads-api.onrender.com/api/comments/setup/${props.setupId}`)
    if (response.ok) {
      comments.value = await response.json()
    }
  } catch (error) {
    console.error('Error loading comments:', error)
  }
}

async function submitComment() {
  if (!newComment.value.trim() || isSubmitting.value) return

  isSubmitting.value = true
  try {
    const response = await fetch(
      `https://gearcrossroads-api.onrender.com/api/comments/setup/${props.setupId}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${userStore.token}`
        },
        body: JSON.stringify({ content: newComment.value })
      }
    )

    if (response.ok) {
      newComment.value = ''
      await loadComments()
      alertStore.show('Comment posted successfully!', 'success')
    } else {
      const error = await response.json()
      alertStore.show(error.message || 'Failed to post comment', 'error')
    }
  } catch (error) {
    console.error('Error posting comment:', error)
    alertStore.show('Failed to post comment', 'error')
  } finally {
    isSubmitting.value = false
  }
}

function startEdit(comment: Comment) {
  editingCommentId.value = comment.id
  editContent.value = comment.content
}

function cancelEdit() {
  editingCommentId.value = null
  editContent.value = ''
}

async function saveEdit(commentId: number) {
  if (!editContent.value.trim() || isSubmitting.value) return

  isSubmitting.value = true
  try {
    const response = await fetch(
      `https://gearcrossroads-api.onrender.com/api/comments/${commentId}`,
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${userStore.token}`
        },
        body: JSON.stringify({ content: editContent.value })
      }
    )

    if (response.ok) {
      editingCommentId.value = null
      editContent.value = ''
      await loadComments()
      alertStore.show('Comment updated successfully!', 'success')
    } else {
      const error = await response.json()
      alertStore.show(error.message || 'Failed to update comment', 'error')
    }
  } catch (error) {
    console.error('Error updating comment:', error)
    alertStore.show('Failed to update comment', 'error')
  } finally {
    isSubmitting.value = false
  }
}

async function deleteComment(commentId: number) {
  commentToDelete.value = commentId
  showDeleteConfirm.value = true
}

async function confirmDelete() {
  if (commentToDelete.value === null) return

  try {
    const response = await fetch(
      `https://gearcrossroads-api.onrender.com/api/comments/${commentToDelete.value}`,
      {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${userStore.token}`
        }
      }
    )

    if (response.ok) {
      await loadComments()
      alertStore.show('Comment deleted', 'success')
    } else {
      alertStore.show('Failed to delete comment', 'error')
    }
  } catch (error) {
    console.error('Error deleting comment:', error)
    alertStore.show('Failed to delete comment', 'error')
  } finally {
    showDeleteConfirm.value = false
    commentToDelete.value = null
  }
}

function cancelDelete() {
  showDeleteConfirm.value = false
  commentToDelete.value = null
}

function canModifyComment(comment: Comment): boolean {
  // Only comment owner can edit
  return comment.userId === currentUserId.value
}

function canDeleteComment(comment: Comment): boolean {
  // Comment owner or admin can delete
  return comment.userId === currentUserId.value || isAdmin.value
}

function getFormattedDate(dateString: string): string {
  // Parse the UTC date and convert to local time
  const date = new Date(dateString)
  
  // Format: MM/DD/YYYY HH:MM AM/PM (in local timezone)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const year = date.getFullYear()
  
  let hours = date.getHours() // This is already in local timezone
  const minutes = String(date.getMinutes()).padStart(2, '0')
  const ampm = hours >= 12 ? 'PM' : 'AM'
  hours = hours % 12
  hours = hours ? hours : 12 // 0 should be 12
  const hoursStr = String(hours).padStart(2, '0')
  
  return `${month}/${day}/${year} ${hoursStr}:${minutes} ${ampm}`
}

onMounted(() => {
  loadComments()
})
</script>
