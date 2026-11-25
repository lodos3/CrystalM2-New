namespace CrystalM2.Rendering.Core;

public readonly record struct ArmourDesign(int Source,int Chest,int Shoulders,int Waist,int Lower,int Palette,int Material)
{
    private static readonly int[] Plate=[1,2,4,5,6];
    private static readonly int[] Robe=[3,7,8,9,10,11];
    public static bool Supports(int id)=>id is >=2 and <=99;
    public static bool IsRobe(int source)=>source is 3 or 7 or 8 or 9 or 10 or 11;
    public int[] Donors=>[Chest,Shoulders,Waist,Lower];

    public static ArmourDesign Resolve(int id)
    {
        if(!Supports(id))throw new ArgumentOutOfRangeException(nameof(id));
        int source=(id-2)%11+1,variant=(id-2)/11;
        int[] family=IsRobe(source)?Robe:Plate;
        int start=Array.IndexOf(family,source);
        // Five/six native sources: rotate the chest and vary the next three
        // without replacement. Nine variants per source are below 5P4.
        // The first family member rotates with the silhouette.
        int[] choices=new int[family.Length];
        for(int i=0;i<choices.Length;i++)choices[i]=family[(start+1+i)%family.Length];
        int code=variant*7;
        Span<int> selected=stackalloc int[4];
        int remaining=choices.Length;
        for(int i=0;i<4;i++)
        {
            int pick=code%remaining;code/=remaining;
            selected[i]=choices[pick];
            for(int j=pick;j<remaining-1;j++)choices[j]=choices[j+1];
            remaining--;
        }
        return new(source,selected[0],selected[1],selected[2],selected[3],
            (source-1+variant*3)%11,variant%4);
    }
}
