import { Component, signal, inject, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { SupportService, DocumentResponse } from '../support.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin.html',
  styleUrls: ['./admin.css']
})
export class AdminComponent implements OnInit {
  private readonly api = inject(SupportService);

  readonly documents = signal<DocumentResponse[]>([]);
  readonly uploading = signal(false);
  readonly message = signal('');
  readonly isError = signal(false);

  newDoc = { title: '', sourceType: 'faq', content: '' };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.listDocuments().subscribe({
      next: (docs) => this.documents.set(docs)
    });
  }

  upload(): void {
    if (!this.newDoc.title.trim() || !this.newDoc.content.trim()) return;

    this.uploading.set(true);
    this.message.set('');
    this.isError.set(false);

    this.api.uploadDocument(this.newDoc.title, this.newDoc.sourceType, this.newDoc.content).subscribe({
      next: (res) => {
        this.uploading.set(false);
        if (res.success) {
          this.message.set(`✅ Uploaded "${this.newDoc.title}" — ${res.chunkCount} chunks embedded`);
          this.newDoc = { title: '', sourceType: 'faq', content: '' };
          this.load();
        } else {
          this.isError.set(true);
          this.message.set(`❌ ${res.error}`);
        }
      },
      error: () => {
        this.uploading.set(false);
        this.isError.set(true);
        this.message.set('Upload failed.');
      }
    });
  }
}