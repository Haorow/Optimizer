namespace Optimizer.Models
{
    /// <summary>
    /// Représente une option de chef d'équipe dans la ComboBox Easy Team.
    /// </summary>
    public class LeaderOption
    {
        public string DisplayName { get; set; } = string.Empty;
        public Personnage? Value { get; set; }
    }
}