using Microsoft.EntityFrameworkCore;
using SuaRevenda.ResourceModels;
using SuaRevenda.Models;
using SuaRevenda.Data;
namespace SuaRevenda.Services;

public class PieceAlreadySoldException : Exception
{
    public PieceAlreadySoldException(Piece piece) : base(
        $"Piece of id = '{piece.Id}' has already been sold"
    )
    { }
}

public class NoSuchSaleException : Exception
{
    public NoSuchSaleException(long id) : base(
        $"Sale of id = '{id}' does not exist"
    )
    { }
}

public class SalesServices
{
    private readonly DataContext _context;

    public SalesServices(DataContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sale>> GetSales()
    {
        var sales = await _context.Sales
            .Include(s => s.Pieces)
            .ToListAsync();

        return sales;
    }

    public async Task<Sale> GetSale(long id)
    {
        var sale = await _context.Sales.FindAsync(id);
        if (sale == null)
        {
            throw new NoSuchSaleException(id);
        }
        return sale;
    }

    public async Task<Sale> SellPieces(CreateSaleSpecification saleSpec)
    {
        long[] piecesInSale = saleSpec.PiecesIds.Select(p => p.Id).ToArray();
        List<Piece> pieces = await _context.Pieces.Where(p => piecesInSale.Contains(p.Id)).ToListAsync();

        ValidatePiecesToSell(pieces);

        var newSale = new Sale
        {
            Price = saleSpec.Price,
            Date = saleSpec.Date,
            Pieces = pieces
        };

        _context.Sales.Add(newSale);
        await _context.SaveChangesAsync();

        return newSale;
    }

    public async Task<Sale> EditSale(SaleUpdateSpecification saleSpec)
    {
        var sale = await GetSale(saleSpec.Id);
        sale.Price = saleSpec.Price;
        sale.Date = saleSpec.Date;
        await _context.SaveChangesAsync();
        return sale;
    }

    private void ValidatePiecesToSell(List<Piece> pieces)
    {
        foreach (var piece in pieces)
        {
            ValidatePieceToSell(piece);
        }
    }

    private void ValidatePieceToSell(Piece piece)
    {
        if (piece.SaleId != null)
        {
            throw new PieceAlreadySoldException(piece);
        }
    }
}
