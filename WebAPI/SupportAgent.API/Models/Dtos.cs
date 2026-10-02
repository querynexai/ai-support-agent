namespace SupportAgent.Api.Models;

// Chat
public record ChatRequest(string Message, Guid? SessionId, int? CustomerId);
public record ChatResponse(string Answer, Guid SessionId, List<SourceCitation> Sources, bool Escalated, string? Error = null);
public record SourceCitation(int DocumentId, string Title, string Snippet, double Similarity);

// Documents
public record DocumentUploadRequest(string Title, string SourceType, string Content, string? SourceUrl);
public record DocumentResponse(int DocumentId, string Title, string SourceType, int ChunkCount, DateTime UploadedAt);

// Admin
public record IngestResult(int DocumentId, int ChunkCount, bool Success, string? Error = null);

// Ticket
public record CreateTicketRequest(int? CustomerId, string Subject, string Description);
public record TicketResponse(int TicketId, string Status);