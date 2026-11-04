using System.Globalization;
using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Modalidades;

public class CriarModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Modalidade Modalidade { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var nome = Modalidade.Nome.Trim();
        if (await NomeEmUsoAsync(db, nome, idAtual: null))
        {
            ModelState.AddModelError("Modalidade.Nome", "Já existe uma modalidade com esse nome.");
            return Page();
        }

        var nova = new Modalidade
        {
            Nome = nome,
            Descricao = Modalidade.Descricao?.Trim(),
            Slug = await SlugUnico.GerarAsync(nome, 80, s => db.Modalidades.AnyAsync(m => m.Slug == s)),
        };
        db.Modalidades.Add(nova);
        await db.SaveChangesAsync();

        TempData["Mensagem"] = $"Modalidade \"{nova.Nome}\" criada.";
        return RedirectToPage("Index");
    }

    /// <summary>
    /// Compara em memória com as regras do português: o lower() do SQLite só trata letras ASCII,
    /// e "JUDÔ" e "judô" seriam vistos como nomes diferentes. A lista de modalidades é pequena.
    /// </summary>
    internal static async Task<bool> NomeEmUsoAsync(AppDbContext db, string nome, int? idAtual) =>
        (await db.Modalidades.Where(m => m.Id != idAtual).Select(m => m.Nome).ToListAsync())
            .Any(n => string.Compare(n, nome, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase) == 0);
}
