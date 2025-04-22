namespace NazihaProject.ViewModels;

public class ProduitIndividuelViewModel
{
    public string Id { get; set; }
    public string Nom { get; set; }
    public string ProduitChimiqueId { get; set; }
}public class ProduitChimiqueViewModel : ArticleViewModel
{
    public List<ProduitIndividuelViewModel> ProduitsIndividuels { get; set; } = new List<ProduitIndividuelViewModel>();
}