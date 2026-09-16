#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the Lesson 76 settings only to the furniture doors and drawers that
/// are missing an InteractiveDoor component. Level doors and static cabinet
/// meshes are deliberately outside this tool's scope.
/// </summary>
public static class ConfigureFurnitureInteractives
{
    private enum FurnitureKind
    {
        KitchenDoor,
        OfficeDoor,
        OfficeDrawer,
        LockerDoor,
        FilingCabinetDrawer
    }

    [MenuItem("Tools/Dead Earth/Configure Cabinet and Drawer Interactions")]
    private static void Configure()
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure furniture interactions");

        AudioCollection cupboardSounds = AssetDatabase.LoadAssetAtPath<AudioCollection>(
            "Assets/Dead Earth/Audios/Audio Collections/Generic Rotating Door Sound.asset");
        AudioCollection drawerSounds = AssetDatabase.LoadAssetAtPath<AudioCollection>(
            "Assets/Dead Earth/Audios/Audio Collections/Drawer Sounds.asset");
        AudioCollection lockerSounds = AssetDatabase.LoadAssetAtPath<AudioCollection>(
            "Assets/Dead Earth/Audios/Audio Collections/Locker Sounds.asset");
        AudioPunchInPunchOutDatabase punchDatabase = AssetDatabase.LoadAssetAtPath<AudioPunchInPunchOutDatabase>(
            "Assets/Dead Earth/Punch In Punch Out Database.asset");

        int configured = 0;
        int kitchenDoorCount = 0;
        int officeDoorCount = 0;
        int officeDrawerCount = 0;
        int lockerDoorCount = 0;
        int filingCabinetDrawerCount = 0;
        var seen = new HashSet<int>();

        foreach (Transform target in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (target == null || !target.gameObject.scene.IsValid() || EditorUtility.IsPersistent(target))
                continue;

            if (!TryGetFurnitureKind(target.name, out FurnitureKind kind) || !seen.Add(target.GetInstanceID()))
                continue;

            InteractiveDoor interactiveDoor = target.GetComponent<InteractiveDoor>();
            if (interactiveDoor == null)
                interactiveDoor = Undo.AddComponent<InteractiveDoor>(target.gameObject);

            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
                collider = Undo.AddComponent<BoxCollider>(target.gameObject);

            Undo.RecordObject(collider, "Configure furniture collider");
            collider.isTrigger = true;

            ConfigureDoor(
                interactiveDoor,
                target,
                kind,
                FindOrCreateContentsMount(target, GetContentsMountName(kind)),
                kind == FurnitureKind.KitchenDoor ? new Vector3(0.0f, -90.0f, 0.0f) :
                kind == FurnitureKind.OfficeDoor || kind == FurnitureKind.LockerDoor ? new Vector3(0.0f, 90.0f, 0.0f) : Vector3.zero,
                kind == FurnitureKind.OfficeDrawer ? new Vector3(0.0f, 0.0f, 0.3f) :
                kind == FurnitureKind.FilingCabinetDrawer ? new Vector3(0.0f, 0.0f, -0.3f) : Vector3.zero,
                kind == FurnitureKind.OfficeDrawer || kind == FurnitureKind.FilingCabinetDrawer ? drawerSounds :
                kind == FurnitureKind.LockerDoor ? lockerSounds : cupboardSounds,
                punchDatabase);

            int interactiveLayer = LayerMask.NameToLayer("Interactive");
            if (interactiveLayer >= 0)
                target.gameObject.layer = interactiveLayer;

            configured++;
            switch (kind)
            {
                case FurnitureKind.KitchenDoor:
                    kitchenDoorCount++;
                    break;
                case FurnitureKind.OfficeDoor:
                    officeDoorCount++;
                    break;
                case FurnitureKind.OfficeDrawer:
                    officeDrawerCount++;
                    break;
                case FurnitureKind.LockerDoor:
                    lockerDoorCount++;
                    break;
                case FurnitureKind.FilingCabinetDrawer:
                    filingCabinetDrawerCount++;
                    break;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Configured {configured} furniture interactables: {kitchenDoorCount} kitchen cabinet doors, {officeDoorCount} office cabinet doors, {officeDrawerCount} office cabinet drawers, {lockerDoorCount} locker doors, and {filingCabinetDrawerCount} filing cabinet drawers.");
    }

    private static bool TryGetFurnitureKind(string objectName, out FurnitureKind kind)
    {
        if (objectName == "Kitchen Cabinet Door")
        {
            kind = FurnitureKind.KitchenDoor;
            return true;
        }

        if (objectName == "Office Cabinet Door" || objectName == "Office Cabinet Door001")
        {
            kind = FurnitureKind.OfficeDoor;
            return true;
        }

        if (objectName == "Office Cabinet Drawer" || objectName == "Office Cabinet Drawer001")
        {
            kind = FurnitureKind.OfficeDrawer;
            return true;
        }

        if (objectName == "Locker Door Green" || objectName == "Locker Door Grey")
        {
            kind = FurnitureKind.LockerDoor;
            return true;
        }

        if (objectName == "FC Top Drawer Green" ||
            objectName == "FC Middle Drawer Green" ||
            objectName == "FC Middle Drawer Grey" ||
            objectName == "FC Bottom Drawer Green" ||
            objectName == "FC Bottom Drawer Grey")
        {
            kind = FurnitureKind.FilingCabinetDrawer;
            return true;
        }

        kind = default;
        return false;
    }

    private static string GetContentsMountName(FurnitureKind kind)
    {
        switch (kind)
        {
            case FurnitureKind.KitchenDoor:
                return "Contents Mount";
            case FurnitureKind.OfficeDoor:
                return "Door Contents Mount";
            case FurnitureKind.LockerDoor:
            case FurnitureKind.FilingCabinetDrawer:
                return "Contents Mount";
            default:
                return "Drawer Contents Mount";
        }
    }

    private static Transform FindOrCreateContentsMount(Transform target, string contentsMountName)
    {
        Transform parent = target.parent != null ? target.parent : target;
        foreach (Transform candidate in parent.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == contentsMountName)
                return candidate;
        }

        var mountObject = new GameObject(contentsMountName);
        Undo.RegisterCreatedObjectUndo(mountObject, "Create furniture contents mount");
        Transform mount = mountObject.transform;
        mount.SetParent(parent, false);
        mount.position = target.position;
        mount.rotation = target.rotation;
        return mount;
    }

    private static void ConfigureDoor(
        InteractiveDoor interactiveDoor,
        Transform target,
        FurnitureKind kind,
        Transform contentsMount,
        Vector3 rotation,
        Vector3 movement,
        AudioCollection sounds,
        AudioPunchInPunchOutDatabase punchDatabase)
    {
        Undo.RecordObject(interactiveDoor, "Configure furniture interaction");

        SerializedObject serialized = new SerializedObject(interactiveDoor);
        serialized.FindProperty("_isClosed").boolValue = true;
        serialized.FindProperty("_isTwoWay").boolValue = false;
        serialized.FindProperty("_autoOpen").boolValue = false;
        serialized.FindProperty("_autoClose").boolValue = false;
        serialized.FindProperty("_autoCloseDelay").vector2Value = new Vector2(5.0f, 5.0f);
        serialized.FindProperty("_disableManualActivation").boolValue = false;
        serialized.FindProperty("_colliderLengthOpenScale").floatValue = 1.0f;
        serialized.FindProperty("_offsetCollider").boolValue = false;
        serialized.FindProperty("_contentsMount").objectReferenceValue = contentsMount;
        serialized.FindProperty("_localForwardAxis").enumValueIndex = (int)InteractiveDoorAxisAlignment.ZAxis;
        serialized.FindProperty("_requiredStates").arraySize = 0;
        serialized.FindProperty("_requiredItems").arraySize = 0;

        string label = kind == FurnitureKind.KitchenDoor ? "Sink Door" :
            kind == FurnitureKind.OfficeDoor ? "Cabinet Door" :
            kind == FurnitureKind.LockerDoor ? "Locker Door" : "Cabinet Drawer";
        if (kind == FurnitureKind.FilingCabinetDrawer)
            label = "Filing Cabinet";
        serialized.FindProperty("_openedHintText").stringValue = $"{label}: Press 'Use' to close";
        serialized.FindProperty("_closedHintText").stringValue = $"{label}: Press 'Use' to open";
        serialized.FindProperty("_cantActivateHintText").stringValue = $"{label}: It's locked";

        SerializedProperty doors = serialized.FindProperty("_doors");
        doors.arraySize = 1;
        SerializedProperty door = doors.GetArrayElementAtIndex(0);
        door.FindPropertyRelative("Transform").objectReferenceValue = target;
        door.FindPropertyRelative("Rotation").vector3Value = rotation;
        door.FindPropertyRelative("Movement").vector3Value = movement;
        door.FindPropertyRelative("ClosedRotation").quaternionValue = Quaternion.identity;
        door.FindPropertyRelative("OpenRotation").quaternionValue = Quaternion.identity;
        door.FindPropertyRelative("OpenPosition").vector3Value = Vector3.zero;
        door.FindPropertyRelative("ClosedPosition").vector3Value = Vector3.zero;

        serialized.FindProperty("_doorSounds").objectReferenceValue = sounds;
        serialized.FindProperty("_audioPunchInPunchOutDatabase").objectReferenceValue = punchDatabase;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(interactiveDoor);
    }
}
#endif
