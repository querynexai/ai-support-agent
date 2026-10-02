import { Component, signal, inject, ChangeDetectionStrategy, ElementRef, viewChild, afterNextRender } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { SupportService, ChatResponse, SourceCitation } from '../support.service';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  sources?: SourceCitation[];
  escalated?: boolean;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chat.html',
  styleUrls: ['./chat.css']
})
export class ChatComponent {
  private readonly api = inject(SupportService);
  private readonly messagesEnd = viewChild<ElementRef<HTMLDivElement>>('messagesEnd');

  readonly messages = signal<Message[]>([]);
  readonly loading = signal(false);
  readonly sessionId = signal<string | undefined>(undefined);
  readonly error = signal('');

  userInput = '';

  readonly suggestions = [
    'What is your return policy?',
    'How long does shipping take?',
    'What is the status of order #3?',
    'Do you offer warranty on electronics?',
    'How do I reset my password?',
    'Can I cancel my order?'
  ];

  constructor() {
    afterNextRender(() => this.scrollToBottom());

    // Welcome message
    this.messages.set([{
      role: 'assistant',
      content: "Hi! I'm QueryNex's AI support assistant. Ask me about orders, policies, products, or anything else — I'm here to help! 👋"
    }]);
  }

  send(): void {
    const text = this.userInput.trim();
    if (!text || this.loading()) return;

    this.messages.update(m => [...m, { role: 'user', content: text }]);
    this.userInput = '';
    this.error.set('');
    this.loading.set(true);

    this.api.chat(text, this.sessionId()).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.error) {
          this.error.set(res.error);
          return;
        }
        if (!this.sessionId()) this.sessionId.set(res.sessionId);
        this.messages.update(m => [...m, {
          role: 'assistant',
          content: res.answer,
          sources: res.sources,
          escalated: res.escalated
        }]);
        this.scrollToBottom();
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to reach support. Please try again.');
      }
    });
  }

  useSuggestion(text: string): void {
    this.userInput = text;
    this.send();
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      this.messagesEnd()?.nativeElement.scrollIntoView({ behavior: 'smooth' });
    }, 50);
  }
}