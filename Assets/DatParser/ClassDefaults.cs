using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class ClassDefaults
    {


        public static CObjectConverter getConv(int parentType, int thisType)
        {
            switch (thisType)
            {
                case 4:
                    {
                        switch (parentType)
                        {
                            case 7319:
                            case 7318:
                            case 602:
                            case 603:
                                return CIntConvertor.inst;
                        }
                        return CFloatConvertor.inst;
                    }
            }

            throw new Exception("Unknown thisTyp[" + thisType + "] and parentType[" + parentType + "]");

            // new CFloatConvertor()
        }
    }
}
