namespace AssetLite.Domain.Enums;

/// <summary>The physical condition of an asset.</summary>
public enum AssetCondition
{
    /// <summary>Brand new.</summary>
    New = 1,

    /// <summary>Used but fully functional with minor wear.</summary>
    Good = 2,

    /// <summary>Functional with noticeable wear.</summary>
    Fair = 3,

    /// <summary>Heavily worn or partially defective.</summary>
    Poor = 4,
}
