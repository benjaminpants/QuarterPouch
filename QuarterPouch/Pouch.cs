using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuarterPouch
{
    public class Pouch
    {
        public Pouch(Items item, string str, double spend, Dictionary<Items, double> converstionRates, bool preserve)
        {
            actingItems = new Items[] { item };
            formatString = str;
            itemConversionRates = converstionRates;
            spendPerUse = spend;
            preserveAfterReset = preserve;
        }

        public Items[] actingItems;
        public bool preserveAfterReset;
        public double spendPerUse;
        public readonly Dictionary<Items, double> itemConversionRates;
        public double amount = 0;
        public string formatString = "${0}";
        public virtual string DisplayString()
        {
            return String.Format(formatString, amount);
        }
        
        // determines if we can add another item to this pouch
        public virtual bool CanFit(Items itm)
        {
            return true;
        }

        public virtual void ResetAmountTo(double amnt)
        {
            amount = amnt;
        }

        public virtual bool AddConversionRateIfAvailable(string name, double amt)
        {
            Items i;
            try
            {
                i = EnumExtensions.GetFromExtendedName<Items>(name);
            }
            catch
            {
                return false;
            }
            itemConversionRates.Add(i, amt);
            return true;
        }

        // Determines if this pouch should allow itself to be spent.
        // You could override this system to create. Idk. Like a credit card or something. That'd be funny.
        // itemUsed is passed so if your pouch supports multiple actingItems
        public virtual bool Spend(Items itemUsed)
        {
            if (amount >= spendPerUse)
            {
                amount -= spendPerUse;
                return true;
            }
            return false;
        }
    }

    // mostly to make creating this thing easy
    public class QuarterPouch : Pouch
    {
        public QuarterPouch() : base(Items.Quarter, "${0}", 0.25, new Dictionary<Items, double>() { { Items.Quarter, 0.25 } }, false)
        {
            AddConversionRateIfAvailable("Gquarter",0.75); // BB Times Compatability
            AddConversionRateIfAvailable("Dollar", 1.00); // Pre-emptive ID stuff
        }

        double myCap => QuarterPouchPlugin.QuarterSizeLimit * 0.25;

        public override string DisplayString()
        {
            return String.Format(formatString, amount.ToString("0.00"));
        }

        public override bool CanFit(Items itm)
        {
            return (amount + itemConversionRates[itm]) <= myCap;
        }

        // if a mod adds a dollar(or some other form of currency) item, and requires it to be used for something, we probably want to subtract a dollar instead.
        public override bool Spend(Items itemUsed)
        {
            double toSpend = itemConversionRates[itemUsed];
            if (amount >= toSpend)
            {
                amount -= toSpend;
                return true;
            }
            return false;
        }
    }
}
