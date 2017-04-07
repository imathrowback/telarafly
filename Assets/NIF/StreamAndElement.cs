using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class StreamAndElement
    {
         public NifMeshStream streamRef;
         public NifStreamElement elem;
         public NiDataStream dataStream;
        public StreamAndElement()
        {

        }
        public StreamAndElement( NifMeshStream streamRef,  NifStreamElement elem2,  NiDataStream dataStream)
        {
            this.streamRef = streamRef;
            elem = elem2;
            this.dataStream = dataStream;
        }
    }
}
