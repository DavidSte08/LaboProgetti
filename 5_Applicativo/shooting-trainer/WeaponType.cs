// Armi disponibili (REQ-011).
public enum WeaponType
{
    Pistola,
    FucileAssalto
}

public static class WeaponTypeExtensions
{
    public static string DisplayName(this WeaponType weapon) => weapon switch
    {
        WeaponType.Pistola => "PISTOLA",
        WeaponType.FucileAssalto => "FUCILE D’ASSALTO",
        _ => weapon.ToString()
    };
}
