import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ChatHealthService, ChatStatus } from '@team-chat-kit/chat';

const statusLabels: Record<ChatStatus, string> = {
  checking: 'Chat module: checking…',
  online: 'Chat module: online',
  offline: 'Chat module: offline',
};

@Component({
  selector: 'tck-home-page',
  template: `
    <section class="max-w-2xl space-y-4">
      <h1 class="text-3xl font-bold tracking-tight">Chat with your team and your partners</h1>
      <p class="text-lg text-slate-600">
        This demo host has its own accounts and organizations. The chat module plugs into it and
        lets people find colleagues and invite contacts from other organizations.
      </p>
      <p class="text-sm text-slate-600" data-testid="chat-status">{{ statusLabel() }}</p>
    </section>
  `,
})
export class HomePage {
  private readonly chatStatus = toSignal(inject(ChatHealthService).check(), {
    initialValue: 'checking',
  });

  protected readonly statusLabel = computed(() => statusLabels[this.chatStatus()]);
}
