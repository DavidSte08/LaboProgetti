// Modalità di gioco selezionabili dal menu (REQ-006).
public enum GameMode
{
    Classica,
    ATempo,
    Precisione
}

public static class GameModeExtensions
{
    public const float TimeLimit = 60f;   // durata della modalità A tempo in secondi (REQ-006_2)

    // Numero di caricatori a disposizione: 0 = illimitati (REQ-006).
    public static int Magazines(this GameMode mode) => mode switch
    {
        GameMode.Classica => 3,
        GameMode.Precisione => 1,
        _ => 0
    };

    // Ricarica permessa? No nella modalità Precisione (REQ-006_3).
    public static bool AllowsReload(this GameMode mode) => mode != GameMode.Precisione;

    public static string DisplayName(this GameMode mode) => mode switch
    {
        GameMode.Classica => "CLASSICA",
        GameMode.ATempo => "A TEMPO",
        GameMode.Precisione => "PRECISIONE",
        _ => mode.ToString()
    };
}
