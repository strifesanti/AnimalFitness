using AnimalFitness.Domain.Accounts;

namespace AnimalFitness.Application.Authorization;

/// <summary>
/// Traduce el rol de membresía (o SuperAdmin) a permisos concretos.
/// </summary>
public static class PermissionMap
{
    private static readonly HashSet<string> OwnerPermissions =
    [
        Permissions.OrganizationManage,
        Permissions.MembersRead,
        Permissions.MembersManage,
        Permissions.InvitationsManage,
        Permissions.JoinRequestsManage,
        Permissions.AthletesRead,
        Permissions.AthletesManage,
        Permissions.TrainersRead,
        Permissions.TrainersManage,
        Permissions.ProfileManageOwn,
        Permissions.ExercisesRead,
        Permissions.ExercisesManage,
        Permissions.WorkoutsRead,
        Permissions.WorkoutsManage,
        Permissions.SessionsRead,
        Permissions.SessionsManageOwn,
        Permissions.ProgressRead,
        Permissions.ProgressManage,
        Permissions.NutritionRead,
        Permissions.NutritionManage,
        Permissions.AttendanceRead,
        Permissions.AttendanceManage,
        Permissions.ChallengesRead,
        Permissions.ChallengesManage,
        Permissions.ChatUse,
        Permissions.NotificationsRead,
    ];

    private static readonly HashSet<string> TrainerPermissions =
    [
        Permissions.MembersRead,
        Permissions.InvitationsManage,
        Permissions.JoinRequestsManage,
        Permissions.AthletesRead,
        Permissions.AthletesManage,
        Permissions.TrainersRead,
        Permissions.ProfileManageOwn,
        Permissions.ExercisesRead,
        Permissions.ExercisesManage,
        Permissions.WorkoutsRead,
        Permissions.WorkoutsManage,
        Permissions.SessionsRead,
        Permissions.SessionsManageOwn,
        Permissions.ProgressRead,
        Permissions.ProgressManage,
        Permissions.NutritionRead,
        Permissions.NutritionManage,
        Permissions.AttendanceRead,
        Permissions.AttendanceManage,
        Permissions.ChallengesRead,
        Permissions.ChallengesManage,
        Permissions.ChatUse,
        Permissions.NotificationsRead,
    ];

    private static readonly HashSet<string> AthletePermissions =
    [
        Permissions.AthletesRead,
        Permissions.ProfileManageOwn,
        Permissions.ExercisesRead,
        Permissions.WorkoutsRead,
        Permissions.SessionsRead,
        Permissions.SessionsManageOwn,
        Permissions.ProgressRead,
        Permissions.ProgressManage,
        Permissions.NutritionRead,
        Permissions.NutritionManage,
        Permissions.AttendanceRead,
        Permissions.ChallengesRead,
        Permissions.ChatUse,
        Permissions.NotificationsRead,
    ];

    private static readonly HashSet<string> SuperAdminPermissions =
    [
        Permissions.TenantsManage,
        Permissions.TenantsRead,
        Permissions.ProfileManageOwn,
    ];

    /// <summary>
    /// Indica si el actor tiene el permiso en el contexto actual.
    /// </summary>
    public static bool Has(string permission, MembershipRole? role, bool isSuperAdmin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        if (isSuperAdmin && SuperAdminPermissions.Contains(permission))
        {
            return true;
        }

        return role switch
        {
            MembershipRole.Owner => OwnerPermissions.Contains(permission),
            MembershipRole.Trainer => TrainerPermissions.Contains(permission),
            MembershipRole.Athlete => AthletePermissions.Contains(permission),
            _ => false,
        };
    }
}
