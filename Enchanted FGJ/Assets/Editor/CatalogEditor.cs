using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


public class CatalogEditor : EditorWindow
{
	/*
	public Catalog craftData;
	private string catalogFilePath = "/StreamingAssets/craftcatalog.json";
	private Vector2 scrollPos;
	public Craftable chooseAspects;
	private bool foundResults = false;
	private bool foundFamiliar = false;
	private bool foundSpell = false;
	private bool foundTalisman = false;
	private bool changesMade = true;
	public Familiar openFamiliar;
	public Spell openSpell;
	public Talisman openTalisman;
	private bool searchedOnce = false;
	private bool resultsCleared = false;
	
	[MenuItem ("Window/Catalog Editor")]
	static void Init()
	{
		CatalogEditor window = (CatalogEditor)EditorWindow.GetWindow (typeof(CatalogEditor));
		window.Show();
	}

	void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
		
		if (craftData == null)
		{
			LoadGameData();
		}

		if (craftData != null)
		{
			SerializedObject serializedObject = new SerializedObject(this);
			
			SerializedProperty type = serializedObject.FindProperty("chooseAspects.type");
			SerializedProperty element = serializedObject.FindProperty("chooseAspects.element");
			SerializedProperty brand = serializedObject.FindProperty("chooseAspects.brand");
			SerializedProperty style = serializedObject.FindProperty("chooseAspects.style");
			SerializedProperty form = serializedObject.FindProperty("chooseAspects.form");

			SerializedProperty editFamiliarName = serializedObject.FindProperty("openFamiliar.name");
			SerializedProperty editFamiliarDescription = serializedObject.FindProperty("openFamiliar.description");
			SerializedProperty editFamiliarNotes = serializedObject.FindProperty("openFamiliar.designNotes");


			SerializedProperty editSpellName = serializedObject.FindProperty("openSpell.name");
			SerializedProperty editSpellDescription = serializedObject.FindProperty("openSpell.description");
			SerializedProperty editSpellNotes = serializedObject.FindProperty("openSpell.designNotes");


			SerializedProperty editTalismanName = serializedObject.FindProperty("openTalisman.name");
			SerializedProperty editTalismanDescription = serializedObject.FindProperty("openTalisman.description");
			SerializedProperty editTalismanNotes = serializedObject.FindProperty("openTalisman.designNotes");



			// DROPDOWNS FOR FINDING OBJECTS BY ASPECT VALUE
			EditorGUILayout.LabelField("Casting Star Aspects:");
			EditorGUILayout.PropertyField(type, true);
			EditorGUILayout.PropertyField(element, true);
			EditorGUILayout.PropertyField(brand, true);
			EditorGUILayout.PropertyField(style, true);
			EditorGUILayout.PropertyField(form, true);


			// SEARCH BUTTON 
			if (GUILayout.Button("Cast Aspects"))
			{
				searchedOnce = true;
				resultsCleared = false;
				foundResults = false;
				foundFamiliar = false;
				foundSpell = false;
				foundTalisman = false;

				chooseAspects.DeadCheck();
				if (!chooseAspects.dead)
				{
					
					int targetLocation = chooseAspects.KeyToLocation(chooseAspects.aspectKey);

					if (chooseAspects.type == TypeAspect.Familiar)
					{
						foreach (Familiar familiar in craftData.allFamiliars)
						{
							if (familiar.uniqueLocation == targetLocation)
							{
								foundResults = true;
								foundFamiliar = true;
								openFamiliar = familiar;
								break;
							}
						}
					}
					
					if (chooseAspects.type == TypeAspect.Spell)
					{
						foreach (Spell spell in craftData.allSpells)
						{
							if (spell.uniqueLocation == targetLocation)
							{
								foundResults = true;
								foundSpell = true;
								openSpell = spell;
								break;
							}
						}
					}

					if (chooseAspects.type == TypeAspect.Talisman)
					{
						foreach (Talisman equip in craftData.allTalismans)
						{
							if (equip.uniqueLocation == targetLocation)
							{
								foundResults = true;
								foundTalisman = true;
								openTalisman = equip;
								break;
							}
						}
					}
				}
			}

			if (foundResults)
			{
				if (foundFamiliar && openFamiliar != null)
				{
					EditorGUILayout.LabelField("Casting Aspects: " + openFamiliar.type.ToString() + " Type | " + openFamiliar.element.ToString() + " Element | " + openFamiliar.brand.ToString() + " Brand | " + openFamiliar.style.ToString() + " Style | " + openFamiliar.form.ToString() + " Form");
					EditorGUILayout.PropertyField(editFamiliarName, true);
					EditorGUILayout.PropertyField(editFamiliarDescription, true);
					EditorGUILayout.LabelField("Familiar-specific variables coming soon.");
					EditorGUILayout.PropertyField(editFamiliarNotes, true);
					EditorGUILayout.LabelField("Unique ID: " + openFamiliar.uniqueLocation);
				}

				if (foundSpell && openSpell != null)
				{
					EditorGUILayout.LabelField("Casting Aspects: " + openSpell.type.ToString() + " Type | " + openSpell.element.ToString() + " Element | " + openSpell.brand.ToString() + " Brand | " + openSpell.style.ToString() + " Style | " + openSpell.form.ToString() + " Form");
					EditorGUILayout.PropertyField(editSpellName, true);
					EditorGUILayout.PropertyField(editSpellDescription, true);
					EditorGUILayout.LabelField("Spell-specific variables coming soon.");
					EditorGUILayout.PropertyField(editSpellNotes, true);
					EditorGUILayout.LabelField("Unique ID: " + openSpell.uniqueLocation);
				}

				if (foundTalisman && openTalisman != null)
				{
					EditorGUILayout.LabelField("Casting Aspects: " + openTalisman.type.ToString() + " Type | " + openTalisman.element.ToString() + " Element | " + openTalisman.brand.ToString() + " Brand | " + openTalisman.style.ToString() + " Style | " + openTalisman.form.ToString() + " Form");
					EditorGUILayout.PropertyField(editTalismanName, true);
					EditorGUILayout.PropertyField(editTalismanDescription, true);
					EditorGUILayout.LabelField("Talisman-specific variables coming soon.");
					EditorGUILayout.PropertyField(editTalismanNotes, true);
					EditorGUILayout.LabelField("Unique ID: " + openTalisman.uniqueLocation);
				}

				if (GUILayout.Button("Clear Results"))
				{
					foundResults = false;
					foundFamiliar = false;
					foundSpell = false;
					foundTalisman = false;
					resultsCleared = true;
				}
			}
			else
			{
				if (searchedOnce && !resultsCleared)
				{
					EditorGUILayout.LabelField("Can't cast with empty slots!");
				}
			}

			

			serializedObject.ApplyModifiedProperties();

			if (changesMade)
			{
				if (GUILayout.Button("Revert Catalog"))
				{
					LoadGameData();
				}
				if (GUILayout.Button("Save Catalog"))
				{
					SaveGameData();
				}
			}
		}

		//if (GUILayout.Button("Load Catalog"))
		//{
		//	LoadGameData();
		//}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	private void LoadGameData()
	{
		string filePath = Application.dataPath + catalogFilePath;
		if (File.Exists (filePath))
		{
			string dataAsJson = File.ReadAllText(filePath);
			craftData = JsonUtility.FromJson<Catalog>(dataAsJson);
		}
		else
		{
			craftData = new Catalog();
		}
		chooseAspects = new Craftable();
	}

	private void SaveGameData()
	{
		string dataAsJson = JsonUtility.ToJson (craftData);
		string filePath = Application.dataPath + catalogFilePath;
		File.WriteAllText(filePath, dataAsJson);
	}
	*/
}
