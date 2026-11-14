using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumericCrossword.Models
{
    public class TemplateJson
    {
        public string name {  get; set; }
        public List<FormulaSlot> slots { get; set; }
    }
}
