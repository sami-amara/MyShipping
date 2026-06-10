using System;
using UI.Helpers;

namespace UI.Helpers
{
    public enum MessageType
    {
        SaveSuccess = 1,
        SaveFailed = 2,
        DeleteSuccess = 3,
        DeleteFailed = 4,
        UpdateSuccess = 5,
        RegistrationSuccess = 6,
        RegistrationFailed = 7,
        UpdateFailed = 8,
        Warning,
        NotFound,
        Success,
        Error,
        Locked,
        UnLocked,
        LockedFaild,
        UnlockedFaild,
        LockedSuccess,
        UnlockedSuccess,
        AdminLocked,
        TempLocked,
        Reactivated,
        Deactivated,


    }
}
