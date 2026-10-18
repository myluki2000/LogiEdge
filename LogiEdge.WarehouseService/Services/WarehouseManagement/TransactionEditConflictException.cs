using System;
using System.Collections.Generic;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    /// <summary>
    /// Thrown when an edit to a booked transaction conflicts with item history created by other
    /// transactions or the Excel importer. Carries one message per affected item so the UI can list
    /// them (see .notes/edit-booked-transactions.md, section 5.4). Nothing is persisted when this is thrown.
    /// </summary>
    public class TransactionEditConflictException : Exception
    {
        public IReadOnlyList<string> Violations { get; }

        public TransactionEditConflictException(IReadOnlyList<string> violations)
            : base(string.Join(" ", violations))
        {
            Violations = violations;
        }
    }
}
