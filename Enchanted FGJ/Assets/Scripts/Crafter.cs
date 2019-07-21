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




public class Crafter : MonoBehaviour
{
	private bool playTesting = true;

	//CATALOG AND TEMPLATE FUNCTIONALITY

	//[HideInInspector]
	public Aspect[] allAspects;
	private bool loadFromExternal = true;
	private bool generateCatalog = false; // should only be needed again if we change aspect names
	
	[HideInInspector]
	public Catalog craftCatalog;

	[HideInInspector]
	public Craftable[] allCraftables;
	private string catalogDataFileName = "craftcatalog.json";



	//CRAFTER AND PLAYER INVENTORY MANAGEMENT

	public Craftable[] playerInventory;

	[HideInInspector]
	public Craftable[] craftingLegacy;
	
	public Craftable[] currentSlots;

	public Craftable currentCraft;

	public float slotRadius;
	public Vector2 screenOutputPosition;

	public Vector2 inventoryBuffer;
	public Vector2 inventoryTopmost;
	public int inventoryColumns;
	public Vector2 inventoryDirection;
	public GameObject inventoryItem;
	public InventoryItem[] craftingVisuals;
	public Transform inventoryBar;

	public string placeholderName;
	public Texture placeholderArt;
	public bool[] occupiedSlots;
	public int[] occupiedKey;
	
	public int inventoryDebugMax;
	
	private int totalCraftables = 1; // move to craft catalog later
	private int totalCraftings = 0;

	private bool slotsInitialized = false;

	public bool grabbing = false;
	public InventoryItem grabbedItem;

	public InventoryItem previewItem;
	public bool previewInitialized;
	public GameObject outputSlot;
	public Texture outputWaiting;
	public Texture outputReady;
	public float currentScroll;
	private float scrollUpper;
	private float scrollLower;
	public GameObject scrollBase;
	public GameObject scrollLid;


	public ColorShift colorShift;


	//UNSLOT all inventory on exit


	//Pass in a masterlist location value and get the final catalog location
	int RealLocation(int location)
	{
		int realLocation = -1;
		bool found = false;
		for (int i = 0; i < craftCatalog.allCraftables.Length; i++)
		{
			if (craftCatalog.allCraftables[i].location == location)
			{
				realLocation = i;
				found = true;
				break;
			}
		}
		
		if (!found)
		{
			Debug.Log("Location conversion messed up.");
		}
		return realLocation;
	}

	//"Instantiate" a new craftable of the correct type using a passed in key
	Craftable CreateFromKey(int[] key)
	{
		bool found = false;
		int realLocation = RealLocation(craftCatalog.template.KeyToLocation(key));
		Craftable newCraftable = new Craftable();
		if (craftCatalog.allCraftables[realLocation].aspectKey[0] == 0)
		{
			newCraftable = new Familiar(craftCatalog.allCraftables[realLocation]);
			found = true;
		}
		if (craftCatalog.allCraftables[realLocation].aspectKey[0] == 1 && !found)
		{
			newCraftable = new Spell(craftCatalog.allCraftables[realLocation]);
			found = true;
		}
		if (craftCatalog.allCraftables[realLocation].aspectKey[0] == 2 && !found)
		{
			newCraftable = new Talisman(craftCatalog.allCraftables[realLocation]);
			found = true;
		}
		if (!found)
		{
			Debug.LogError("Create New Craftable failed to find with provided key.");
		}
		return newCraftable;
	}


	Craftable RandomCraftable() // add lock-ins and limits later
	{
		int[] newKey = new int[allAspects.Length];
		for (int i = 0; i < newKey.Length; i++)
		{
			newKey[i] = (int)Mathf.Floor(Random.Range(0, allAspects[i].sigils.Length)); //when did they make the max inclusive? yuck
			if(newKey[i] >= allAspects[i].sigils.Length) // JUST IN CASE!
			{
				newKey[i] = allAspects[i].sigils.Length - 1;
			}
		}
		return CreateFromKey(newKey);
	}


	//TEST function, create 5 random objects, mark one as bonded to simulate starting experience
	void GenerateStartingInventory()
	{
		playerInventory = new Craftable[allAspects.Length];
		for (int i = 0; i < playerInventory.Length; i++)
		{
			playerInventory[i] = RandomCraftable();
			totalCraftables += 1;
			playerInventory[i].unique = totalCraftables;
		}
	}

	//Combine two Arrays of Craftables (usually to add to inventory)
	Craftable[] MergeCraftLists(Craftable[] addList, Craftable[] originalList)
	{
		Craftable[] rebuiltList = new Craftable[originalList.Length + addList.Length];
		int rebuiltCount = 0;

		for (int i = 0; i < addList.Length; i++)
		{
			rebuiltList[rebuiltCount] = addList[i];
			rebuiltCount += 1;
		}
		
		for (int i = 0; i < originalList.Length; i++)
		{
			rebuiltList[rebuiltCount] = originalList[i];
			rebuiltCount += 1;
		}
		return rebuiltList;
	}


	Craftable[] AddToCraftableList(Craftable newCraftable, Craftable[] list)
	{
		Craftable[] newCraftableList = new Craftable[list.Length + 1];
		newCraftableList[0] = newCraftable;
		for (int i = 1; i < newCraftableList.Length; i++)
		{
			newCraftableList[i] = list[i-1];
		}
		return newCraftableList;
	}


	//TEST function, fill inventory list back up to ten
	/*void RefillInventory()
	{
		if(playerInventory.Length < inventoryDebugMax && totalCraftings > 0)
		{
			int dif = inventoryDebugMax - playerInventory.Length;
			Craftable[] randomCraftables = new Craftable[dif];
			for (int i = 0; i < randomCraftables.Length; i++)
			{
				randomCraftables[i] = RandomCraftable();
				totalCraftables += 1;
				randomCraftables[i].unique = totalCraftables;
			}
			playerInventory = MergeCraftLists(randomCraftables, playerInventory);
			GenerateAllItems();
		}
	}*/

	public bool CheckIfFull()
	{
		bool slotDoubleCheck = false;
		for (int i = 0; i < occupiedSlots.Length; i++)
		{
			if (!occupiedSlots[i])
			{	
				slotDoubleCheck = false;
				break;
			}
			else
			{
				slotDoubleCheck = true;
			}
		}
		return slotDoubleCheck;
	}

	public void FinalizeCraftSlots()
	{
		if(CheckIfFull())
		{
			craftingLegacy = MergeCraftLists(currentSlots, craftingLegacy);

			//RemoveFromCraftList currentSlots

			Craftable newCraftable = CreateFromKey(currentCraft.aspectKey);
			totalCraftables += 1;
			newCraftable.unique = totalCraftables;
			playerInventory = AddToCraftableList(newCraftable, playerInventory);
			InventoryItem newCraftableItem = CreateInventoryItem(newCraftable);
			AddInventoryItem(newCraftableItem);

			ClearAllSlots();
			totalCraftings += 1;
		}
	}


	public void ClearAllSlots ()
	{
		for (int i = 0; i < allAspects.Length; i++)
		{
			ClearSlot(i, true);
		}
	}

	public void ClearSlot(int slotAspect, bool wipe = false)
	{
		if (occupiedSlots[slotAspect])
		{
			occupiedSlots[slotAspect] = false;
			currentSlots[slotAspect].isSlotted = false;
			UpdatePreview();
			allAspects[slotAspect].slotInterface.GetComponent<Renderer>().material.mainTexture = allAspects[slotAspect].emptyArt;
		}
		else
		{
			if (!wipe)
			{
				Debug.LogError("Trying to clear an already empty slot.");
			}
		}
	}


	public void FillSlot(int slotAspect, Craftable droppedCraftable)
	{
		if (!slotsInitialized)
		{
			slotsInitialized = true;
			currentSlots = new Craftable[allAspects.Length];
			occupiedSlots = new bool[allAspects.Length];
			occupiedKey = new int[allAspects.Length];
		}

		if(occupiedSlots[slotAspect])
		{
			ClearSlot(slotAspect);
		}
		occupiedSlots[slotAspect] = true;

		currentSlots[slotAspect] = droppedCraftable;
		currentSlots[slotAspect].isSlotted = true;
		currentSlots[slotAspect].slottedInAspect = slotAspect;

		occupiedKey[slotAspect] = currentSlots[slotAspect].aspectKey[slotAspect];
		
		allAspects[slotAspect].slotInterface.GetComponent<Renderer>().material.mainTexture = allAspects[slotAspect].sigils[occupiedKey[slotAspect]].fullArt;

		UpdatePreview();

	}



	void UpdatePreview()
	{
		if (CheckIfFull())
		{
			currentCraft = craftCatalog.allCraftables[RealLocation(craftCatalog.template.KeyToLocation(occupiedKey))];
			if (previewInitialized)
			{
				previewItem.craftable = currentCraft;
				previewItem.InitializeVisuals();
			}
			else
			{
				previewInitialized = true;
				previewItem = CreateInventoryItem(currentCraft, true);
			}
			outputSlot.GetComponent<Renderer>().material.mainTexture = outputReady;
		}
		else
		{
			if (previewInitialized)
			{
				previewInitialized = false;
				Destroy(previewItem.gameObject);
				outputSlot.GetComponent<Renderer>().material.mainTexture = outputWaiting;
			}
		}
	}


	public int BarCounter ()
	{
		int barCount = 0;
		for (int i = 0; i < craftingVisuals.Length; i++)
		{
			if(craftingVisuals[i].barSlot > -1 && !craftingVisuals[i].isPreview && craftingVisuals[i].location == InventoryItem.Location.Bar)
			{
				barCount += 1;
			}
		}
		return barCount;
	}

	public void UpdateBarPosition(InventoryItem toUpdate)
	{
		Vector3 barPos = inventoryBar.TransformPoint(new Vector3(inventoryTopmost.x * Mathf.Pow(inventoryBuffer.x, toUpdate.barSlot), inventoryTopmost.y + (inventoryBuffer.y * toUpdate.barSlot), 0));
		toUpdate.transform.position = new Vector3(barPos.x, barPos.y, toUpdate.transform.position.z);
	}

	public void RemoveFromInventoryBar(int removeAt)
	{
		for (int i = 0; i < craftingVisuals.Length; i++)
		{
			if(!craftingVisuals[i].isPreview && craftingVisuals[i].location == InventoryItem.Location.Bar)
			{
				if (craftingVisuals[i].barSlot > removeAt)
				{
					craftingVisuals[i].barSlot -= 1;
				}
				UpdateBarPosition(craftingVisuals[i]);
			} 
		}
	}

	public void AddToInventoryBar (int addAt)
	{
		for (int i = 0; i < craftingVisuals.Length; i++)
		{
			if(!craftingVisuals[i].isPreview && craftingVisuals[i].location == InventoryItem.Location.Bar)
			{
				if (craftingVisuals[i].barSlot >= addAt)
				{
					craftingVisuals[i].barSlot += 1;
				}
				UpdateBarPosition(craftingVisuals[i]);
			} 
		}
	}



	InventoryItem CreateInventoryItem (Craftable linkedItem, bool setPreview = false)
	{
		GameObject newItem = Instantiate(inventoryItem);
		newItem.GetComponent<InventoryItem>().LinkItem(linkedItem, allAspects, this, setPreview);
		return newItem.GetComponent<InventoryItem>();
	}


	public void AddInventoryItem(InventoryItem newItem)
	{
		InventoryItem[] newCraftingVisuals = new InventoryItem[craftingVisuals.Length + 1];
		newCraftingVisuals[craftingVisuals.Length] = newItem;
		for (int i = 0; i < craftingVisuals.Length; i++)
		{
			newCraftingVisuals[i] = craftingVisuals[i];
		}
		craftingVisuals = newCraftingVisuals;
	}


	void GenerateAllItems ()
	{
		if (craftingVisuals.Length > 0)
		{
			foreach(InventoryItem item in craftingVisuals)
			{
				Destroy(item.gameObject);
			}
			ClearAllSlots();
		}

		craftingVisuals = new InventoryItem[playerInventory.Length];	
		for (int i = 0; i < playerInventory.Length; i++)
		{
			craftingVisuals[i] = CreateInventoryItem(playerInventory[i]);
			craftingVisuals[i].barSlot = i;
			UpdateBarPosition(craftingVisuals[i]);
		}
	}


	void Awake()
	{
		if (GameObject.FindWithTag("RainbowManager"))
		{
			ColorShift rainbowManager = GameObject.FindWithTag("RainbowManager").GetComponent<ColorShift>();
			colorShift = rainbowManager;
		}

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


		if (playTesting)
		{
			GenerateStartingInventory();
		}

		GenerateAllItems();


	}

	void Update()
	{
		/*
		if (playTesting)
		{
			if (Input.GetKeyDown("space"))
			{
				RefillInventory();
			}
			//ESCAPE = CLOSE
			//?? = RESTART SCENE
		}

		
		if (grabbing)
		{
			foreach (InventoryItem item in craftingVisuals)
			{
				if (!item.grabbed && Vector2.Distance(grabbedItem.transform.position, item.transform.position) < grabbedItem.grabRadius)
				{

				}
			}
		}
		*/

		if (currentScroll < 0)
		{
		}
		


	}


	private void GenerateCatalog() //prune power-based lookup system results to just the fully defined aspects
	{	
		int masterKeySize = allAspects.Length;
		for (int k = 0; k < allAspects.Length; k++)
		{
			if (allAspects[k].sigils.Length > allAspects.Length)
			{
				masterKeySize = allAspects[k].sigils.Length; // if any aspects have more total values than the total number of aspects we want to use the largest
				Debug.LogError("Not set up to handle catalog with more sigils than total aspects.");
			}
		}

		Craftable template = new Craftable();
		template.masterKeySize = masterKeySize;
		
		int craftableTotal = (int)Mathf.Pow(masterKeySize, masterKeySize); 

		allCraftables = new Craftable[craftableTotal]; // create the master list
		
		int finalCraftables = 0;
		for (int i = 0; i < allCraftables.Length; i++)
		{
			allCraftables[i] = new Craftable();
			allCraftables[i].masterKeySize = masterKeySize;
			int[] newKeys = template.LocationToKey(i);
			allCraftables[i].SetKey(newKeys, allAspects);
			allCraftables[i].location = template.KeyToLocation(allCraftables[i].aspectKey);
			allCraftables[i].DeadCheck(allAspects);
			if (!allCraftables[i].dead)
			{
				finalCraftables += 1;
			}
		}

		craftCatalog = new Catalog();
		craftCatalog.allCraftables = new Craftable[finalCraftables];
		int craftCount = 0;

		for (int j = 0; j < allCraftables.Length; j++)
		{
			if (!allCraftables[j].dead)
			{
				int addedCheck = craftCount;
				if (allCraftables[j].aspectKey[0] == 0)
				{
					craftCatalog.allCraftables[craftCount] = new Familiar(allCraftables[j]);
					craftCount += 1;
				}
				if (allCraftables[j].aspectKey[0] == 1)
				{
					craftCatalog.allCraftables[craftCount] = new Spell(allCraftables[j]);
					craftCount += 1;
				}
				if (allCraftables[j].aspectKey[0] == 2)
				{
					craftCatalog.allCraftables[craftCount] = new Talisman(allCraftables[j]);
					craftCount += 1;
				}
				if (craftCount > addedCheck)
				{
					craftCatalog.allCraftables[craftCount-1].SetAspectNames(allAspects);
				}
			}
		}

		craftCatalog.template = template;
		craftCatalog.allGuidelines = new Guideline[0];
		craftCatalog.playerInventory = new Craftable[0];

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

// ************************************************** ASPECT CLASS **************************************************
[System.Serializable]
public class Aspect
{
	public string name;
	public Sigil[] sigils;
	public Vector2 slotPosition;
	public Vector2 itemPosition;
	public Texture emptyArt;
	public GameObject slotInterface;

}
// ************************************************** SIGIL CLASS **************************************************
[System.Serializable] 
public class Sigil
{
	public string name;
	//public string flavorText;
	public Texture fullArt;
	public Texture icon;
	public Color color;
}


 // ************************************************** CRAFTABLE CLASS **************************************************
[System.Serializable]
public class Craftable
{

	//CATALOG VARIABLES
	[HideInInspector]
	public bool dead = false;

	public string name;

	//[ReadOnly]
	public string[] aspectNames;
	
	[HideInInspector]
	public int[] aspectKey;

	public int location;


	public int masterKeySize;
	
	public Texture image;
	[TextArea] public string description;
	[TextArea] public string designNotes;



	//INVENTORY-ONLY VARIABLES
	public bool bonded;
	public int slottedInAspect;
	public bool isSlotted;
	public int legacyUnique;
	public int unique;
	public bool isDust;

	



	public void SetKey (int[] key, Aspect[] aspectMaster)
	{
		aspectKey = new int[aspectMaster.Length];

		for (int i = 0; i < aspectKey.Length; i++)
		{
			aspectKey[i] = key[i];
		}
	}

	public void SetAspectNames (Aspect[] aspectMaster)
	{
		if (!dead)
		{
			aspectNames = new string[aspectMaster.Length];

			for (int i = 0; i < aspectKey.Length; i++)
			{
				aspectNames[i] = aspectMaster[i].name + ": " + aspectMaster[i].sigils[aspectKey[i]].name;
			}
		}
		else
		{
			Debug.LogError("Trying to add aspect names to Dead Craftable.");
		}
	}
	


	// ENTER KEY, GET ARRAY POSITION OF OBJECT
	public int KeyToLocation (int[] key)
	{
		float size = masterKeySize;
		float newLocation = 0f;
		for (int i = 0; i < key.Length; i++)
		{

			//might need to add some checks here if things break

			newLocation += (Mathf.Pow(size,(size-i))/size)*key[i];
		}
		return (int) newLocation;
	}

	// REVERSE LOOKUP: ENTER POSITION IN ARRAY, GET ACTUAL KEY VALUES
	public int[] LocationToKey (int checkLocation)
	{
		int[] key = new int[masterKeySize];
		int prevDims = 0;

		for (int i = masterKeySize -1; i >= 0; i--)
		{
			int newDim = (checkLocation-prevDims)%(int)Mathf.Pow(masterKeySize,masterKeySize-i);
			prevDims += newDim;
			key[i] = newDim/((int)Mathf.Pow(masterKeySize,masterKeySize-i)/masterKeySize);
		}

		return key;
	}

	// ACTUAL CRAFTING LOOKUP: USE LOCATIONS IN KEY ARRAY AS INPUT OBJECTS TO FIND NEW LOCATION ID OF OUTPUT OBJECT
	//not sure if i actually need this one anymore, maybe only for editor
	public int CombineLocations (int[] locations)
	{
		int[] newKey = new int[locations.Length];
		for (int i = 0; i < locations.Length; i++)
		{
			newKey[i]=LocationToKey(locations[i])[i];
		}
		return KeyToLocation(newKey);
	}

	public void DeadCheck (Aspect[] aspectMaster)
	{

		for (int i = 0; i < aspectKey.Length; i++)
		{
			if (aspectKey[i] < 0 || aspectKey[i] > aspectMaster[i].sigils.Length - 1)
			{
				dead = true;
				break;
			}
		}

		/*
		if (aspectKey.Length < masterKeySize)
		{

		}
		*/
	}

}


 // ************************************************** FAMILIAR CLASS **************************************************
[System.Serializable]
public class Familiar : Craftable
{
	public string nickname;
	public Familiar (Craftable craftable)
	{
		if (craftable.dead)
		{
			Debug.LogError("Trying to add Dead Craftable to Catalog.");
		}
		else
		{
			name = craftable.name;
			masterKeySize = craftable.masterKeySize;
			aspectKey = craftable.aspectKey;
			aspectNames = craftable.aspectNames;
			location = craftable.location;
		}
	}

}

 // ************************************************** SPELL CLASS **************************************************
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
			name = craftable.name;
			masterKeySize = craftable.masterKeySize;
			aspectKey = craftable.aspectKey;
			aspectNames = craftable.aspectNames;
			location = craftable.location;
		}
	}
}


 // ************************************************** TALISMAN CLASS **************************************************
[System.Serializable]
public class Talisman : Craftable
{
	public Talisman (Craftable craftable)
	{
		if (craftable.dead)
		{
			Debug.LogError("Trying to add Dead Craftable to Catalog.");
		}
		else
		{
			name = craftable.name;
			masterKeySize = craftable.masterKeySize;
			aspectKey = craftable.aspectKey;
			aspectNames = craftable.aspectNames;
			location = craftable.location;
		}
	}
}


 // ************************************************** GUIDELINE CLASS **************************************************
[System.Serializable]
public class Guideline
{
	public bool[] relevantKeys;
	public int[] keyValues;
	public string guidelineText;
}

 // ************************************************** CATALOG CLASS **************************************************
[System.Serializable]
public class Catalog
{
	public Craftable template;
	public Craftable[] allCraftables;
	public Craftable[] playerInventory;
	public Craftable[] craftingLegacy;
	public Guideline[] allGuidelines;
}






 // ************************************************** READONLY PROPERTY **************************************************
 /*
 public class ReadOnlyAttribute : PropertyAttribute
 {
 
 }
 
 [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
 public class ReadOnlyDrawer : PropertyDrawer
 {
	 public override float GetPropertyHeight(SerializedProperty property,
											 GUIContent label)
	 {
		 return EditorGUI.GetPropertyHeight(property, label, true);
	 }
 
	 public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
	 {
		 GUI.enabled = false;
		 EditorGUI.PropertyField(position, property, label, true);
		 GUI.enabled = true;
	 }
 }
 */