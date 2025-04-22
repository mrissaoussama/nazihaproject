namespace NazihaProject.ViewModels;

public class StockViewModel
{
    public string Id { get; set; }
    public string Nom { get; set; }
    public List<ArticleViewModel> Articles { get; set; } = new List<ArticleViewModel>();
}