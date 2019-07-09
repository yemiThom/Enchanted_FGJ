using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEngine.SceneManagement;

/*

TODO

Create class for combination rules
	Array of rules in inspector
	Add array of rules to json
	Display relevant rules in Catalog Editor even when slots are empty


*/

public enum TypeAspect {Empty, Familiar, Spell, Equip};
public enum BrandAspect {Empty, Affordable, Classy, Indie};
public enum ElementAspect {Empty, Earth, Water, Fire, Wind, Light};
public enum StyleAspect {Empty, Active, Passive};
public enum FormAspect {Empty, Basic, Evolved};


public class Crafter : MonoBehaviour
{
	private int keyTotal = 5;
	private bool loadFromExternal = true;
	private bool generateCatalog = false; // should only be needed again if we change aspect names or values
	public Catalog craftCatalog;
	public Craftable[] allCraftables;
	private string catalogDataFileName = "craftcatalog.json";

    void Start()
    {
    	if (loadFromExternal)
    	{
    		LoadCatalogData();
    	}
    	else
    	{
    		if (generateCatalog)
    		{
    			GenerateCatalog();
    		}
    		else
    		{
    			Debug.LogError("Catalog missing! Load from external, locate missing json, or generate from scratch.");
    		}
    	}
    }

    private void GenerateCatalog() //prune power-based lookup system results to just the fully defined aspects
    {
    	int craftableTotal = (int)Mathf.Pow(keyTotal, keyTotal);
    	int finalCraftables = 0;
    	allCraftables = new Craftable[craftableTotal];
    	Craftable template = new Craftable();

    	for (int i = 0; i < allCraftables.Length; i++)
    	{
    		allCraftables[i] = new Craftable();
    		int[] newKeys = template.LocationToKeys(i);
    		allCraftables[i].SetAspects(newKeys);
    		int[] testKeys = allCraftables[i].GetKeys();
    		allCraftables[i].uniqueLocation = template.KeysToLocation(testKeys);
    		allCraftables[i].DeadCheck();
    		if (!allCraftables[i].dead)
    		{
    			finalCraftables += 1;
    		}
    	}

    	int typeSize = (int)(finalCraftables/(System.Enum.GetValues(typeof(TypeAspect)).Length-1));
    	craftCatalog.allFamiliars = new Familiar[typeSize];
    	int familiarCount = 0;
      	
      	craftCatalog.allSpells = new Spell[typeSize];
    	int spellCount = 0;

    	craftCatalog.allEquips = new Equip[typeSize];
		int equipCount = 0;

		for (int j = 0; j < allCraftables.Length; j++)
    	{
    		if (!allCraftables[j].dead)
    		{
    			if (allCraftables[j].type == TypeAspect.Familiar)
    			{
    				craftCatalog.allFamiliars[familiarCount] = new Familiar(allCraftables[j]);
    				familiarCount += 1;
    			}
    			if (allCraftables[j].type == TypeAspect.Spell)
    			{
    				craftCatalog.allSpells[spellCount] = new Spell(allCraftables[j]);
    				spellCount += 1;
    			}
    			if (allCraftables[j].type == TypeAspect.Equip)
    			{
    				craftCatalog.allEquips[equipCount] = new Equip(allCraftables[j]);
    				equipCount += 1;
    			}
    		}
    	}
    	string catalogFilePath = "/StreamingAssets/" + catalogDataFileName;
    	string dataAsJson = JsonUtility.ToJson (craftCatalog);
		string filePath = Application.dataPath + catalogFilePath;
		File.WriteAllText(filePath, dataAsJson);


    }


	private void LoadCatalogData()
	{
		string filePath = Path.Combine(Application.streamingAssetsPath, catalogDataFileName);
		if (File.Exists(filePath))
		{
			string dataAsJson = File.ReadAllText(filePath);
			craftCatalog = JsonUtility.FromJson<Catalog>(dataAsJson);
		}
		else
		{
			Debug.LogError("Can't find crafting catalog!");
		}
	}
}



[System.Serializable]
public class Craftable
{
	private int keyTotal = 5;
	[HideInInspector] public bool dead = false;
	public TypeAspect type;
	public BrandAspect brand;
	public ElementAspect element;
	public StyleAspect style;
	public FormAspect form;
	[HideInInspector] public int uniqueLocation;
	public string name;
	[TextArea] public string description;
	[TextArea] public string designNotes;

	public int[] GetKeys ()
	{
		int[] key = new int[5];
		key[0] = (int)type - 1;
		key[1] = (int)brand - 1;
		key[2] = (int)element - 1;
		key[3] = (int)style - 1;
		key[4] = (int)form - 1;
		return key;
	}

	public void SetAspects (int[] key)
	{
		type = (TypeAspect)(key[0] + 1);
		brand = (BrandAspect)(key[1] + 1);
		element = (ElementAspect)(key[2] + 1);
		style = (StyleAspect)(key[3] + 1);
		form = (FormAspect)(key[4] + 1);
	}

    // ENTER KEY, GET ARRAY POSITION OF OBJECT
	public int KeysToLocation (int[] key)
    {
    	float size = key.Length;
    	float location = 0f;
    	for (int i = 0; i < key.Length; i++)
    	{
    		if (key[i] > keyTotal - 1)
    		{
    			key[i] = keyTotal - 1; // unmapped key value, this shouldn't happen
    			Debug.LogError("KEY VALUE TOO BIG");
    		}
    		if (key[i] < 0)
    		{
    			key[i] = 0; // negative key value, this definitely shouldn't happen
    			Debug.LogError("KEY VALUE TOO SMALL");
    		}
    		location += (Mathf.Pow(size,(size-i))/size)*key[i];
    	}
    	return (int) location;
    }

    // REVERSE LOOKUP: ENTER POSITION IN ARRAY, GET ACTUAL KEY VALUES
    public int[] LocationToKeys (int location)
    {
    	int[] key = new int[keyTotal];
    	int prevDims = 0;

    	for (int i = keyTotal -1; i >= 0; i--)
    	{
    		int newDim = (location-prevDims)%(int)Mathf.Pow(keyTotal,keyTotal-i);
    		prevDims += newDim;
    		key[i] = newDim/((int)Mathf.Pow(keyTotal,keyTotal-i)/keyTotal);
    	}

    	return key;
    }

    //ACTUAL CRAFTING LOOKUP: USE LOCATIONS IN KEY ARRAY AS INPUT OBJECTS TO FIND NEW LOCATION ID OF OUTPUT OBJECT
    public int CombineLocations (int[] locations)
    {
    	int[] newKey = new int[locations.Length];
    	for (int i = 0; i < locations.Length; i++)
    	{
    		newKey[i]=LocationToKeys(locations[i])[i];
    	}
    	return KeysToLocation(newKey);
    }

	public void DeadCheck ()
	{
		if ((int)type >= System.Enum.GetValues(typeof(TypeAspect)).Length ||
		(int)brand >= System.Enum.GetValues(typeof(BrandAspect)).Length ||
		(int)element >= System.Enum.GetValues(typeof(ElementAspect)).Length ||
		(int)style >= System.Enum.GetValues(typeof(StyleAspect)).Length ||
		(int)form >= System.Enum.GetValues(typeof(FormAspect)).Length ||
		(int)type <= 0 ||
		(int)brand <= 0 ||
		(int)element <= 0 ||
		(int)style <= 0 ||
		(int)form <= 0)
		{
			dead = true;
		}
		else
		{
			dead = false;
		}
	}

}


[System.Serializable]
public class Familiar : Craftable
{
	public Familiar (Craftable craftable)
	{
		if (craftable.dead)
		{
			Debug.LogError("Trying to add Dead Craftable to Catalog.");
		}
		else
		{
			type = craftable.type;
			brand = craftable.brand;
			element = craftable.element;
			style = craftable.style;
			form = craftable.form;
			uniqueLocation = craftable.uniqueLocation;
		}
	}

}

[System.Serializable]
public class Spell : Craftable
{
	public Spell (Craftable craftable)
	{
		if (craftable.dead)
		{
			Debug.LogError("Trying to add Dead Craftable to Catalog.");
		}
		else
		{
			type = craftable.type;
			brand = craftable.brand;
			element = craftable.element;
			style = craftable.style;
			form = craftable.form;
			uniqueLocation = craftable.uniqueLocation;
		}
	}
}

[System.Serializable]
public class Equip : Craftable
{
	public Equip (Craftable craftable)
	{
		if (craftable.dead)
		{
			Debug.LogError("Trying to add Dead Craftable to Catalog.");
		}
		else
		{
			type = craftable.type;
			brand = craftable.brand;
			element = craftable.element;
			style = craftable.style;
			form = craftable.form;
			uniqueLocation = craftable.uniqueLocation;
		}
	}
}


[System.Serializable]
public class Catalog
{
	public Familiar[] allFamiliars;
	public Spell[] allSpells;
	public Equip[] allEquips;
}