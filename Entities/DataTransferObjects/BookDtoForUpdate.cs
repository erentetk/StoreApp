using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DataTransferObjects
{

    //Eger bir Data transfer objects yazıyorsal
    /*
     * readonly olmalı
     * immutable olmalı değiştirilemez olmalı
     * LINQ destegi vardır
     * Ref Type olur 
     * Ctor ile DTO tanımlama  şansı verir
     */

    public record BookDtoForUpdate(int Id, String Title, decimal Price);

    //public record BookDtoForUpdate
    //{
    //    public int Id { get; init; } //init denilince readonly olur tanımlandıgı yerde sadece set edilir. 
    //    public String Title { get; init; }
    //    public decimal Price { get; init; }
    //}
}
