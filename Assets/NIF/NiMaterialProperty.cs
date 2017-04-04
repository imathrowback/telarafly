using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiMaterialProperty : NiProperty
    {
        public Point4f matAmbient;
        public Point4f matDiffuse;
        public Point4f matSpecular;
        public Point4f matEmit;
        public float matShine;
        public float matAlpha;
        public bool hasMaterialProps;

    public override void parse( NIFFile file,  NIFObject baseo,  BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            matAmbient = new Point4f(ds.readFloat(), ds.readFloat(), ds.readFloat(), 1.0f);
		matDiffuse = new Point4f(ds.readFloat(), ds.readFloat(), ds.readFloat(), 1.0f);
		matSpecular = new Point4f(ds.readFloat(), ds.readFloat(), ds.readFloat(), 1.0f);
		matEmit = new Point4f(ds.readFloat(), ds.readFloat(), ds.readFloat(), 1.0f);
		matShine = ds.readFloat();
		matAlpha = ds.readFloat();
		hasMaterialProps = true;

	}

}
}
