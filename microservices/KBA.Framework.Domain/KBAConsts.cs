namespace KBA.Framework.Domain;

/// <summary>
/// Constantes globales du framework KBA pour les microservices
/// </summary>
public static class KBAConsts
{
    /// <summary>
    /// Préfixe pour toutes les tables de la base de données
    /// </summary>
    public const string TablePrefix = "KBA.";

    /// <summary>
    /// Longueur maximale par défaut pour les champs de texte courts
    /// </summary>
    public const int MaxNameLength = 256;

    /// <summary>
    /// Longueur maximale pour les descriptions
    /// </summary>
    public const int MaxDescriptionLength = 1024;

    /// <summary>
    /// Longueur maximale pour les emails
    /// </summary>
    public const int MaxEmailLength = 256;

    /// <summary>
    /// Longueur maximale pour les numéros de téléphone
    /// </summary>
    public const int MaxPhoneLength = 24;
}
