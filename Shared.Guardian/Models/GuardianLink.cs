using Shared.Enums;
using Shared.Models;

namespace Shared.Guardian.Models;

public class GuardianLink : BaseEntity
{
    public Guid GuardianUserId { get; set; }
    public Guid WardUserId { get; set; }
    public GuardianPermission Permissions { get; set; }
}
