using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Application.Purchases;

public interface IPurchaseCsvParser
{
    CsvParseResult Parse(Stream csvStream);
}
