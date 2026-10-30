// Modalità di gioco selezionabili dal menu (REQ-006).
public enum GameMode
{
    Classica,
    ATempo,
    Precisione
}

public static class GameModeExtensions
{
    public static string DisplayName(this GameMode mode) => mode switch
    {
        GameMode.Classica => "CLASSICA",
        GameMode.ATempo => "A TEMPO",
        GameMode.Precisione => "PRECISIONE",
        _ => mode.ToString()
    };
}
