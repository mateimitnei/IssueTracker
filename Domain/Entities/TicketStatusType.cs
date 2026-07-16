namespace Domain.Entities;

public enum TicketStatusType : byte {
    TODO = 1,
    IN_PROGRESS = 2,
    IN_REVIEW = 3,
    DONE = 4
}