import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface SourceCitation {
  documentId: number;
  title: string;
  snippet: string;
  similarity: number;
}

export interface ChatResponse {
  answer: string;
  sessionId: string;
  sources: SourceCitation[];
  escalated: boolean;
  error?: string | null;
}

export interface DocumentResponse {
  documentId: number;
  title: string;
  sourceType: string;
  chunkCount: number;
  uploadedAt: string;
}

export interface IngestResult {
  documentId: number;
  chunkCount: number;
  success: boolean;
  error?: string | null;
}

@Injectable({ providedIn: 'root' })
export class SupportService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiBase;

  chat(message: string, sessionId?: string, customerId?: number): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.apiBase}/chat`, { message, sessionId, customerId });
  }

  listDocuments(): Observable<DocumentResponse[]> {
    return this.http.get<DocumentResponse[]>(`${this.apiBase}/documents`);
  }

  uploadDocument(title: string, sourceType: string, content: string): Observable<IngestResult> {
    return this.http.post<IngestResult>(`${this.apiBase}/documents`, { title, sourceType, content });
  }
}