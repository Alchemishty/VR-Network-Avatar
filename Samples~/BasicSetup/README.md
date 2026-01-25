# VRNetworkAvatar - Basic Setup Guide

This guide walks you through setting up VR networked avatars using the VRNetworkAvatar module.

## Prerequisites

- Unity 2022.3 LTS or newer
- Netcode for GameObjects 1.5+
- TextMeshPro
- An XR SDK (Meta XR SDK or Unity XR Interaction Toolkit)

## Quick Start

### 1. Scene Setup

1. Create a new scene or open your existing network scene
2. Add a **NetworkManager** if you don't have one already
3. Create an empty GameObject named "VRNetworkAvatarManager"
4. Add the following components to it:
   - `NetworkObject`
   - `VRNetworkAvatarManager`
   - `NetworkAvatarSpawner`
5. Add the VRNetworkAvatarManager to NetworkManager's Network Prefabs list

### 2. Create Avatar Prefab

**Important**: Prefabs must be created in Unity Editor. Create a new prefab for your networked avatar:

```
NetworkAvatar (GameObject)
├── NetworkObject
├── ClientAuthorityNetworkTransform
├── NetworkPlayerIdentity
├── NetworkVRAvatar
│
├── Head (Transform - your head mesh)
├── LeftHand (Transform - your left hand mesh)
├── RightHand (Transform - your right hand mesh)
└── Body (Transform - your body mesh)
```

Configure the `NetworkVRAvatar` component:
- Assign the Head, LeftHand, RightHand, and Body transforms
- Adjust position/rotation offsets as needed for your avatar model

### 3. XR Rig Setup

Add an XR rig provider component to your XR rig:

**For Meta/Oculus:**
- Add `OVRRigProvider` to your OVRCameraRig
- The component will auto-detect controllers and head tracking

**For Unity XR Interaction Toolkit:**
- Add `GenericXRRigProvider` to your XR Origin
- Assign references if auto-detection doesn't work

### 4. Register Prefab with NetworkManager

1. Select your avatar prefab
2. Open the NetworkManager
3. Add the prefab to the "Network Prefabs" list

### 5. Configure Spawner

1. Select your VRNetworkAvatarManager GameObject
2. In the `NetworkAvatarSpawner` component, assign your avatar prefab
3. Optionally add spawn point transforms

## Component Reference

### VRNetworkAvatarManager
Central manager for all network avatars. Handles:
- Client connection/disconnection tracking
- Avatar registry
- Generic events for extensions

### NetworkPlayerIdentity
Handles player identity (display name) synchronization.
- Call `SetDisplayName(string)` to update the player's name
- Subscribe to `DisplayName.OnValueChanged` for name updates

### NetworkVRAvatar
Synchronizes VR tracking data (head, hands, body).
- Automatically finds XR rig provider in scene
- Applies arm length and head rotation constraints
- Uses thresholds to reduce network traffic

### AvatarFingerSync (Optional)
Add this component for finger tracking support.
- Only works with XR providers that support finger tracking
- Separate from NetworkVRAvatar for modularity

### ClientAuthorityNetworkTransform
Client-authoritative NetworkTransform for VR.
- The client controls their avatar's position
- Essential for VR where client has physical tracking data

## Events

### VRNetworkAvatarManager Events
```csharp
// Client events
OnClientConnected(ulong clientId)
OnClientDisconnected(ulong clientId)

// Avatar events
OnAvatarRegistered(ulong clientId, GameObject avatar)
OnAvatarUnregistered(ulong clientId, GameObject avatar)

// Generic event for extensions
OnAvatarEvent(ulong clientId, int eventId)
```

### NetworkAvatarSpawner Events
```csharp
// Custom prefab selection
OnSelectAvatarPrefab = (clientId, defaultPrefab) => { return customPrefab; }

// Post-spawn hook
OnAvatarSpawned(ulong clientId, GameObject avatar)
```

## Customization

### Sync Settings
Create a new `AvatarSyncSettings` asset:
1. Right-click in Project window
2. Create > VRNetworkAvatar > Sync Settings
3. Adjust thresholds and constraints
4. Assign to `NetworkVRAvatar` component

### Avatar Visuals
Replace the meshes in your avatar prefab's Head, LeftHand, RightHand, and Body transforms.

### Custom XR Provider
Implement `IXRRigProvider` interface for custom XR SDKs:
```csharp
public class MyXRProvider : MonoBehaviour, IXRRigProvider
{
    public Transform HeadTransform => ...;
    public Transform LeftHandTransform => ...;
    public Transform RightHandTransform => ...;
    // ... implement other members
}
```

## Troubleshooting

### Avatar not moving
- Check that `NetworkVRAvatar.IsOwner` is true for local player
- Verify XR rig provider is initialized (`IsInitialized = true`)
- Check XR rig transforms are assigned correctly

### Avatar spawning at wrong position
- Add spawn points to `NetworkAvatarSpawner`
- Or set initial position in prefab

### Hands stretching too far
- Reduce `maxArmLength` in sync settings or NetworkVRAvatar

### Head tilting unnaturally
- Reduce `headRotationClampAngle` in sync settings

## Platform-Specific Notes

### Meta Quest
- Ensure Meta XR SDK is installed
- `OVRRigProvider` will auto-detect OVRCameraRig
- Finger tracking requires hand tracking enabled in device settings

### Unity XR
- Works with any device supported by XR Interaction Toolkit
- `GenericXRRigProvider` doesn't support finger tracking
- Manually assign controller transforms if auto-detection fails
