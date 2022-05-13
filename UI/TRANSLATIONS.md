# Translations

## How to add a new language:

Open up the file `TranslatedLanguages.cs`

In the `enum`, add your new language to the bottom of the enum `TranslatedLanguages`.

In the `enum` extension `TranslatedLanguagesExtensions`, in the method `Culture`, add your new language Culture Info. For Swedish, this would be "sv-SE", for Spanish, it would be "es-ES". 

If you want to see every existing culture code, you can run this line of code:
```c#
CultureInfo[] cinfo = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);
```
You can also just google a languages culture code, it's all documented online.

## Creating a new translation file

Create a language file by cloning the `English.txt` file and renaming it to your desired language (Make sure it is named the same as the `enum` option added earlier).

You will then need to attach this file to the Translation prefab. Open the ModIoBrowser prefab in the Unity browser, and then find and click on "Translations". Drag the new file into the serialized list `Translations Text Assets`.

## Working with the new translation file

When you open up the translation file that you just created, you are going to see a header with some meta-data, followed by lines that look like this:
```
(In English.txt)
msgid "Play"
msgstr "Play"
```
`msgid` is the id or key that we use to find the translation.
`msgstr` is the translated string.

The translation in Swedish would correspond to this:
```
(In Swedish.txt)
msgid "Play"
msgstr "Spela"
```
As you can see, we only change the `msgstr` value.

Some fields look like this:
`msgstr "Current users {users_number} are playing the mod {mod_name}!"`

In this case, when you fetch this translation, you can input the values for `users_number` and `mod_name` via code.

Avoid editing old translations, instead add new keys to avoid issues. Finding every place where a translation has been used can be cumbersome, time consuming, and prone to breaking implemented features.

## How to add a new translation key:

Simply copy paste the `msgid` and `msgstr` to the bottom of the file and fill in the values that you want.

## Working with the Translation code.

There are a couple of different scenarios that the translation code needs to cover:

1. **Texts residing in the Unity scene**

    Add the script `Translatable` to the object. The Translatable script will automatically find any `TextMeshProUGUI` text components and attach to it. Any text inside the `TextMeshProUGUI` will automatically be used as a key in your translations file - make sure the `msgid` that corresponds to the field exists in all your translation files.
   
    If you do not use `TextMeshProUGUI` components to display text, you will need to extend the functionality of the `Translatable.cs` file. The file is documented, and there is a short description on how to accomplish this on the variable named `text`.

    In some cases, you don't want to add scripts to certain objects, as it may mess with Unity.

    In that case, you can also drag the game object which contains a `TextMeshProUGUI` element on to the serialized list `Translated`, on the Translations game object.

    If the user changes language during runtime, this text will automatically be updated.


2. **Texts residing in the C# code and replacing keywords**

    Sometimes you want to update text by code, Eg. to support displaying numbers such as: `"Current players: 56".`
	
    If that is the case, you can do the following.

    Translation string: 
    ```
   msgid: "Installed mods"
    msgstr: "Installed mods: {player_number}"
   ```

    Code:

    ```c#
   Translation myTranslation; // the pointer to the translation
    TMP_Text translationText; //the text element to contain the translation
    int playerNumber; //whatever number we want to input
   ```

    Now we can call this whenever the text needs updating:

   ```c#
   Translation.Get(myTranslation, "Installed mods", translationText, playerNumber.ToString());
   ```

    If the user changes language during runtime, this text will automatically be updated.	
	
    C# may complain that "The compiler detected an uninitialized private or internal field declaration that is never assigned a value". 
    This is wrong. The value is used as a pointer, and is assigned a value inside Translation.Get. 

    If you want to get rid of these warnings, use 
```c#
#pragma warning disable 0649
    Translation myTranslation;
    TMP_Text translationText;
    int playerNumber;
#pragma warning restore 0649
```


## Done!

That's it. Thank you for reading!
