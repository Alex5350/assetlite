using AssetLite.Domain.Enums;

namespace AssetLite.Infrastructure.Persistence;

/// <summary>
/// Static development seed catalog: 7 offices (HQ → regions → sites), 7 categories and 45 assets
/// with realistic names, purchase data over the last three years (plus older retired gear) and
/// assignment histories including returned entries. All data is fixed — no randomness — so seeded
/// databases are reproducible.
/// </summary>
internal static class SeedData
{
    /// <summary>
    /// Office tree: (name, code, parent code or null for the root). Codes are uppercase
    /// alphanumeric per the Office aggregate's rule (3-8 chars, no dashes), hence e.g.
    /// <c>ASTHQ</c> rather than <c>AST-HQ</c>.
    /// </summary>
    public static readonly (string Name, string Code, string? ParentCode)[] Offices =
    [
        ("Headquarters", "ASTHQ", null),
        ("East Region", "ASTEAST", "ASTHQ"),
        ("West Region", "ASTWEST", "ASTHQ"),
        ("New York Site", "ASTNYC", "ASTEAST"),
        ("Boston Site", "ASTBOS", "ASTEAST"),
        ("San Francisco Site", "ASTSFO", "ASTWEST"),
        ("Los Angeles Site", "ASTLAX", "ASTWEST"),
    ];

    /// <summary>Categories: (name, description, expected lifespan in months).</summary>
    public static readonly (string Name, string Description, int LifespanMonths)[] Categories =
    [
        ("Laptops", "Portable computers issued to staff.", 36),
        ("Desktops", "Workstation and desktop computers.", 48),
        ("Monitors", "External displays and docks with displays.", 48),
        ("Tablets", "iPads and Android tablets.", 36),
        ("Phones", "Company mobile phones.", 24),
        ("Networking", "Switches, routers, firewalls and access points.", 60),
        ("Peripherals", "Keyboards, mice, hubs and other accessories.", 36),
    ];

    /// <summary>The 45 seeded assets, ordered by their sequential tag numbers.</summary>
    public static readonly SeedAsset[] Assets =
    [
        // --- Laptops (12) -----------------------------------------------------
        New(1, "Dell Latitude 5540", "ASTNYC", "Laptops", AssetCondition.Good, new DateOnly(2024, 3, 12), 1149.00m,
            "Dell", "Latitude 5540", "5CG1430ZQ2",
            history: [Assigned("Sarah Chen", "sarah.chen@assetlite.example", D(2024, 3, 20), D(2024, 9, 2)),
                      Assigned("Marcus Webb", "marcus.webb@assetlite.example", D(2024, 9, 10), null)]),
        New(2, "MacBook Pro 14 M4", "ASTNYC", "Laptops", AssetCondition.New, new DateOnly(2025, 1, 28), 2399.00m,
            "Apple", "MacBook Pro 14", "C02XK1YZJGH5",
            history: [Assigned("Sarah Chen", "sarah.chen@assetlite.example", D(2025, 2, 3), null)]),
        New(3, "Dell Latitude 5450", "ASTNYC", "Laptops", AssetCondition.New, new DateOnly(2025, 4, 2), 1249.00m,
            "Dell", "Latitude 5450", "7HJ2K34LMN"),
        New(4, "ThinkPad X1 Carbon G12", "ASTBOS", "Laptops", AssetCondition.Good, new DateOnly(2024, 8, 19), 1899.00m,
            "Lenovo", "ThinkPad X1 Carbon Gen 12", "PF3RTY8U2Q",
            history: [Assigned("Priya Raman", "priya.raman@assetlite.example", D(2024, 8, 26), null)]),
        New(5, "MacBook Air 15 M3", "ASTBOS", "Laptops", AssetCondition.Good, new DateOnly(2024, 6, 5), 1499.00m,
            "Apple", "MacBook Air 15", "C02YF3KL2Q",
            history: [Assigned("Nia Okafor", "nia.okafor@assetlite.example", D(2024, 6, 12), D(2025, 1, 15))],
            finalState: SeedFinalState.Maintenance, stateChangedAt: D(2025, 1, 20),
            notes: "Keyboard replacement under warranty."),
        New(6, "HP EliteBook 860 G11", "ASTBOS", "Laptops", AssetCondition.New, new DateOnly(2025, 2, 14), 1599.00m,
            "HP", "EliteBook 860 G11", "5CD2479KMS"),
        New(7, "Dell XPS 15 9530", "ASTSFO", "Laptops", AssetCondition.Good, new DateOnly(2024, 10, 1), 2049.00m,
            "Dell", "XPS 15 9530", "9BXQ71LM2K",
            history: [Assigned("Daniel Ortiz", "daniel.ortiz@assetlite.example", D(2024, 10, 7), null)]),
        New(8, "MacBook Pro 16 2019", "ASTSFO", "Laptops", AssetCondition.Poor, new DateOnly(2020, 6, 11), 2799.00m,
            "Apple", "MacBook Pro 16", "C08TR2QM9X",
            history: [Assigned("Ravi Patel", "ravi.patel@assetlite.example", D(2020, 6, 15), null)],
            finalState: SeedFinalState.Retired, stateChangedAt: D(2024, 12, 5),
            notes: "Battery swelling; withdrawn from service."),
        New(9, "ThinkPad T14s G5", "ASTSFO", "Laptops", AssetCondition.New, new DateOnly(2025, 5, 8), 1399.00m,
            "Lenovo", "ThinkPad T14s Gen 5", "PL9M2K4X7T"),
        New(10, "Surface Laptop 7 13.8", "ASTLAX", "Laptops", AssetCondition.New, new DateOnly(2025, 3, 17), 1699.00m,
            "Microsoft", "Surface Laptop 7", "0FJ4KD72XC",
            history: [Assigned("Aisha Bello", "aisha.bello@assetlite.example", D(2025, 3, 24), null)]),
        New(11, "Dell Precision 5490", "ASTLAX", "Laptops", AssetCondition.Good, new DateOnly(2024, 11, 6), 2499.00m,
            "Dell", "Precision 5490", "8XN4P0TQ3V"),
        New(12, "MacBook Pro 13 2017", "ASTHQ", "Laptops", AssetCondition.Poor, new DateOnly(2017, 5, 30), 1999.00m,
            "Apple", "MacBook Pro 13", "C02QW7E1JN",
            history: [Assigned("James Holt", "james.holt@assetlite.example", D(2017, 6, 5), D(2023, 8, 11))],
            finalState: SeedFinalState.Disposed, stateChangedAt: D(2023, 8, 15)),

        // --- Desktops (5) -----------------------------------------------------
        New(13, "Dell OptiPlex 7010 SFF", "ASTHQ", "Desktops", AssetCondition.Good, new DateOnly(2023, 10, 24), 1099.00m,
            "Dell", "OptiPlex 7010", "4LO9X2M8KP",
            history: [Assigned("James Holt", "james.holt@assetlite.example", D(2023, 11, 1), D(2024, 5, 20)),
                      Assigned("Elena Ford", "elena.ford@assetlite.example", D(2024, 5, 21), null)],
            notes: "Front desk workstation."),
        New(14, "HP EliteDesk 800 G9", "ASTHQ", "Desktops", AssetCondition.New, new DateOnly(2024, 9, 9), 1199.00m,
            "HP", "EliteDesk 800 G9", "2CC81K5MR7"),
        New(15, "iMac 24 M4", "ASTNYC", "Desktops", AssetCondition.New, new DateOnly(2025, 2, 3), 1699.00m,
            "Apple", "iMac 24", "C02ZP4LN8QT",
            history: [Assigned("Chloe Dubois", "chloe.dubois@assetlite.example", D(2025, 2, 10), D(2025, 6, 1)),
                      Assigned("Tom Larsen", "tom.larsen@assetlite.example", D(2025, 6, 2), null)],
            notes: "Design studio machine."),
        New(16, "Dell Precision 3680", "ASTBOS", "Desktops", AssetCondition.Good, new DateOnly(2024, 4, 18), 1849.00m,
            "Dell", "Precision 3680", "3KJ71PX4N9",
            history: [Assigned("Priya Raman", "priya.raman@assetlite.example", D(2024, 4, 25), null)],
            finalState: SeedFinalState.Maintenance, stateChangedAt: D(2025, 3, 10),
            notes: "GPU fan failure."),
        New(17, "Dell OptiPlex 3050", "ASTSFO", "Desktops", AssetCondition.Fair, new DateOnly(2018, 8, 15), 749.00m,
            "Dell", "OptiPlex 3050", "6YU2M8QR4L",
            history: [Assigned("Ravi Patel", "ravi.patel@assetlite.example", D(2018, 9, 1), D(2023, 5, 19))],
            finalState: SeedFinalState.Retired, stateChangedAt: D(2023, 5, 25)),

        // --- Monitors (9) -----------------------------------------------------
        New(18, "Dell UltraSharp U2723QE", "ASTNYC", "Monitors", AssetCondition.Good, new DateOnly(2023, 11, 20), 579.99m,
            "Dell", "U2723QE", "CN0D7V2M0K1",
            history: [Assigned("Marcus Webb", "marcus.webb@assetlite.example", D(2023, 11, 27), null)]),
        New(19, "Dell UltraSharp U2723QE", "ASTNYC", "Monitors", AssetCondition.Good, new DateOnly(2024, 1, 16), 579.99m,
            "Dell", "U2723QE", "CN0D7V2M0K2"),
        New(20, "Apple Studio Display", "ASTNYC", "Monitors", AssetCondition.New, new DateOnly(2023, 12, 8), 1599.00m,
            "Apple", "Studio Display", "F17LN03QXK",
            history: [Assigned("Tom Larsen", "tom.larsen@assetlite.example", D(2023, 12, 15), null)]),
        New(21, "LG UltraFine 27UN850", "ASTBOS", "Monitors", AssetCondition.Good, new DateOnly(2024, 2, 27), 449.99m,
            "LG", "27UN850-W", "404MKAS0YZ",
            history: [Assigned("Priya Raman", "priya.raman@assetlite.example", D(2024, 3, 4), null)]),
        New(22, "Samsung Odyssey G7 27", "ASTBOS", "Monitors", AssetCondition.Good, new DateOnly(2023, 9, 14), 529.99m,
            "Samsung", "Odyssey G7 27", "0TJ4K72XQ9"),
        New(23, "Dell UltraSharp U3423WE", "ASTSFO", "Monitors", AssetCondition.Fair, new DateOnly(2023, 7, 25), 949.99m,
            "Dell", "U3423WE", "CN0K9F3L7P2",
            history: [Assigned("Daniel Ortiz", "daniel.ortiz@assetlite.example", D(2023, 8, 1), null)],
            finalState: SeedFinalState.Maintenance, stateChangedAt: D(2025, 4, 14),
            notes: "Panel flicker; awaiting replacement part."),
        New(24, "Dell UltraSharp U2724D", "ASTSFO", "Monitors", AssetCondition.New, new DateOnly(2024, 8, 30), 429.99m,
            "Dell", "U2724D", "CN0M1X8R3T6"),
        New(25, "LG 27UN880-B", "ASTLAX", "Monitors", AssetCondition.Good, new DateOnly(2024, 5, 21), 379.99m,
            "LG", "27UN880-B", "505NKW9QR7",
            history: [Assigned("Aisha Bello", "aisha.bello@assetlite.example", D(2024, 5, 28), null)]),
        New(26, "HP Z27", "ASTHQ", "Monitors", AssetCondition.Poor, new DateOnly(2019, 4, 11), 549.00m,
            "HP", "Z27", "CNC8041ZL2",
            history: [Assigned("Elena Ford", "elena.ford@assetlite.example", D(2019, 4, 18), null)],
            finalState: SeedFinalState.Disposed, stateChangedAt: D(2024, 11, 2)),

        // --- Tablets (5) ------------------------------------------------------
        New(27, "iPad Pro 11 M4", "ASTNYC", "Tablets", AssetCondition.New, new DateOnly(2025, 4, 22), 999.00m,
            "Apple", "iPad Pro 11", "DMPZQ3LNXK1",
            history: [Assigned("Marcus Webb", "marcus.webb@assetlite.example", D(2025, 4, 28), null)]),
        New(28, "iPad Air 11 M2", "ASTBOS", "Tablets", AssetCondition.New, new DateOnly(2024, 7, 30), 599.00m,
            "Apple", "iPad Air 11", "DYG7K2LM4RT"),
        New(29, "Galaxy Tab S9", "ASTSFO", "Tablets", AssetCondition.Good, new DateOnly(2024, 3, 19), 799.99m,
            "Samsung", "Galaxy Tab S9", "R9WT30K2XQ7",
            history: [Assigned("Daniel Ortiz", "daniel.ortiz@assetlite.example", D(2024, 3, 25), D(2024, 11, 8)),
                      Assigned("Grace Kim", "grace.kim@assetlite.example", D(2024, 11, 12), null)]),
        New(30, "iPad Pro 13 M4", "ASTHQ", "Tablets", AssetCondition.New, new DateOnly(2025, 6, 10), 1299.00m,
            "Apple", "iPad Pro 13", "DMPZR8QK4N2",
            notes: "Boardroom kit."),
        New(31, "iPad 10th Gen", "ASTLAX", "Tablets", AssetCondition.Fair, new DateOnly(2023, 10, 5), 449.00m,
            "Apple", "iPad 10", "DYK2LM4RQ8X",
            history: [Assigned("Aisha Bello", "aisha.bello@assetlite.example", D(2023, 10, 12), null)],
            finalState: SeedFinalState.Maintenance, stateChangedAt: D(2025, 6, 3),
            notes: "Battery replacement."),

        // --- Phones (6) -------------------------------------------------------
        New(32, "iPhone 15 Pro", "ASTNYC", "Phones", AssetCondition.Good, new DateOnly(2024, 1, 9), 999.00m,
            "Apple", "iPhone 15 Pro", "F2LXQ8KM3T",
            history: [Assigned("Sarah Chen", "sarah.chen@assetlite.example", D(2024, 1, 15), null)]),
        New(33, "iPhone 15", "ASTBOS", "Phones", AssetCondition.Good, new DateOnly(2024, 4, 3), 799.00m,
            "Apple", "iPhone 15", "F2LQR7MT9K",
            history: [Assigned("Nia Okafor", "nia.okafor@assetlite.example", D(2024, 4, 8), D(2024, 12, 20)),
                      Assigned("James Holt", "james.holt@assetlite.example", D(2025, 1, 6), null)]),
        New(34, "Pixel 8 Pro", "ASTSFO", "Phones", AssetCondition.Good, new DateOnly(2024, 2, 15), 899.00m,
            "Google", "Pixel 8 Pro", "GP8X31KQ7R2"),
        New(35, "Pixel 9 Pro", "ASTLAX", "Phones", AssetCondition.New, new DateOnly(2025, 5, 29), 999.00m,
            "Google", "Pixel 9 Pro", "GP9X72M4T8",
            history: [Assigned("Aisha Bello", "aisha.bello@assetlite.example", D(2025, 6, 4), null)]),
        New(36, "iPhone 12", "ASTHQ", "Phones", AssetCondition.Poor, new DateOnly(2021, 10, 22), 799.00m,
            "Apple", "iPhone 12", "F17GQ2MX7L",
            history: [Assigned("Elena Ford", "elena.ford@assetlite.example", D(2021, 10, 29), null)],
            finalState: SeedFinalState.Retired, stateChangedAt: D(2025, 2, 14)),
        New(37, "Galaxy S24", "ASTNYC", "Phones", AssetCondition.New, new DateOnly(2024, 6, 25), 749.00m,
            "Samsung", "Galaxy S24", "RFS24K7MQ3"),

        // --- Networking (4) ---------------------------------------------------
        New(38, "Cisco Catalyst 9300-24T", "ASTHQ", "Networking", AssetCondition.Good, new DateOnly(2023, 8, 30), 6500.00m,
            "Cisco", "Catalyst 9300", "FDO2342A1BX",
            history: [Assigned("Ravi Patel", "ravi.patel@assetlite.example", D(2023, 9, 5), null)],
            notes: "Core switch, rack A1."),
        New(39, "UniFi Switch Pro 24", "ASTNYC", "Networking", AssetCondition.New, new DateOnly(2024, 9, 26), 499.00m,
            "Ubiquiti", "Switch Pro 24", "FCA830KD2M6"),
        New(40, "Cisco Meraki MX67", "ASTSFO", "Networking", AssetCondition.Fair, new DateOnly(2023, 5, 17), 1200.00m,
            "Cisco", "Meraki MX67", "Q2XX-N8K3PL",
            history: [Assigned("Daniel Ortiz", "daniel.ortiz@assetlite.example", D(2023, 5, 24), null)],
            finalState: SeedFinalState.Maintenance, stateChangedAt: D(2025, 7, 8),
            notes: "Firmware upgrade in progress."),
        New(41, "UniFi U6 Pro AP", "ASTBOS", "Networking", AssetCondition.New, new DateOnly(2024, 12, 12), 189.00m,
            "Ubiquiti", "U6 Pro", "FCB729KE4T8"),

        // --- Peripherals (4) --------------------------------------------------
        New(42, "Logitech MX Keys S", "ASTHQ", "Peripherals", AssetCondition.New, new DateOnly(2024, 10, 14), 119.99m,
            "Logitech", "MX Keys S", "28KQ71MX3P"),
        New(43, "Dell KM7321W Combo", "ASTNYC", "Peripherals", AssetCondition.Good, new DateOnly(2024, 8, 8), 89.99m,
            "Dell", "KM7321W", "CN0M2K71XQ4",
            history: [Assigned("Marcus Webb", "marcus.webb@assetlite.example", D(2024, 8, 15), null)]),
        New(44, "Logitech MX Master 3S", "ASTBOS", "Peripherals", AssetCondition.New, new DateOnly(2024, 7, 1), 99.99m,
            "Logitech", "MX Master 3S", "29T4K8MS7Q2"),
        New(45, "Anker 341 USB-C Hub", "ASTLAX", "Peripherals", AssetCondition.New, new DateOnly(2025, 1, 20), 59.99m,
            "Anker", "341 USB-C Hub", "24J71KA9MX3",
            history: [Assigned("Aisha Bello", "aisha.bello@assetlite.example", D(2025, 1, 27), null)]),
    ];

    private static SeedAsset New(
        int tagNumber,
        string name,
        string officeCode,
        string categoryName,
        AssetCondition condition,
        DateOnly purchaseDate,
        decimal purchaseCost,
        string manufacturer,
        string model,
        string serialNumber,
        SeedAssignment[]? history = null,
        SeedFinalState finalState = SeedFinalState.InStock,
        DateTimeOffset? stateChangedAt = null,
        string? notes = null) =>
        new(tagNumber, name, officeCode, categoryName, condition, purchaseDate, purchaseCost, manufacturer, model, serialNumber,
            history ?? [], finalState, stateChangedAt, notes);

    private static SeedAssignment Assigned(string name, string email, DateTimeOffset assignedAtUtc, DateTimeOffset? returnedAtUtc) =>
        new(name, email, assignedAtUtc, returnedAtUtc);

    private static DateTimeOffset D(int year, int month, int day) => new(year, month, day, 9, 0, 0, TimeSpan.Zero);
}

/// <summary>Final lifecycle state of a seeded asset after replaying its history.</summary>
internal enum SeedFinalState
{
    /// <summary>In stock, never assigned or returned.</summary>
    InStock,

    /// <summary>Currently assigned (history ends with an open assignment).</summary>
    Assigned,

    /// <summary>Under maintenance (state change closes any open assignment).</summary>
    Maintenance,

    /// <summary>Retired (state change closes any open assignment).</summary>
    Retired,

    /// <summary>Retired then disposed.</summary>
    Disposed,
}

/// <summary>One assignment history entry to replay on a seeded asset.</summary>
/// <param name="AssigneeName">Assignee display name.</param>
/// <param name="AssigneeEmail">Assignee email.</param>
/// <param name="AssignedAtUtc">Hand-over moment.</param>
/// <param name="ReturnedAtUtc">Return moment, or null to leave the assignment open.</param>
internal sealed record SeedAssignment(
    string AssigneeName,
    string AssigneeEmail,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? ReturnedAtUtc);

/// <summary>A seeded asset definition (see <see cref="SeedData"/>).</summary>
/// <param name="TagNumber">Sequential tag number (1-based).</param>
/// <param name="Name">Display name.</param>
/// <param name="OfficeCode">Office code holding the asset.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Condition">Physical condition.</param>
/// <param name="PurchaseDate">Purchase date.</param>
/// <param name="PurchaseCost">Purchase cost (USD).</param>
/// <param name="Manufacturer">Manufacturer.</param>
/// <param name="Model">Model.</param>
/// <param name="SerialNumber">Serial number.</param>
/// <param name="History">Assignment history to replay, in order.</param>
/// <param name="FinalState">Lifecycle state after the history replay.</param>
/// <param name="StateChangedAtUtc">Moment of the final-state transition (maintenance/retirement).</param>
/// <param name="Notes">Optional notes.</param>
internal sealed record SeedAsset(
    int TagNumber,
    string Name,
    string OfficeCode,
    string CategoryName,
    AssetCondition Condition,
    DateOnly PurchaseDate,
    decimal PurchaseCost,
    string Manufacturer,
    string Model,
    string SerialNumber,
    SeedAssignment[] History,
    SeedFinalState FinalState,
    DateTimeOffset? StateChangedAtUtc,
    string? Notes);
