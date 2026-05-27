using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Domain.Invoices
{
    public enum InvoiceStatus
    {
        Pending,           
        PartiallyAccepted,  
        Accepted,           
        Cancelled           
    }
}
