using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class UpdateControllerAfterDelete
{
    [MenuItem("Tools/Rebuild Animator For 3-Hit Combo")]
    public static void Rebuild()
    {
        string controllerPath = "Assets/MovementTest/MidasController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("JumpAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ComboStep", AnimatorControllerParameterType.Int);

        var sm = controller.layers[0].stateMachine;

        // Load clips
        AnimationClip idleClip = LoadClip("Assets/Deneme Assets/Idle.fbx");
        AnimationClip runClip = LoadClip("Assets/Deneme Assets/midas@Fast Run.fbx");
        AnimationClip jumpClip = LoadClip("Assets/Deneme Assets/Jumping.fbx");
        AnimationClip a1Clip = LoadClip("Assets/Deneme Assets/attack1.fbx");
        AnimationClip a2Clip = LoadClip("Assets/Deneme Assets/attack2.fbx");
        AnimationClip a3Clip = LoadClip("Assets/Deneme Assets/attack3.fbx");
        AnimationClip jaClip = LoadClip("Assets/Deneme Assets/Jump Attack.fbx");

        // Create States
        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        var runState = sm.AddState("Run");
        runState.motion = runClip;

        var jumpState = sm.AddState("Jump");
        jumpState.motion = jumpClip;

        var a1State = sm.AddState("Attack1");
        a1State.motion = a1Clip;
        a1State.speed = 1.35f;

        var a2State = sm.AddState("Attack2");
        a2State.motion = a2Clip;
        a2State.speed = 1.35f;

        var a3State = sm.AddState("Attack3");
        a3State.motion = a3Clip;
        a3State.speed = 1.25f;

        var jaState = sm.AddState("JumpAttack");
        jaState.motion = jaClip;
        jaState.speed = 1.3f;

        // Locomotion transitions
        var toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Jump transitions
        var anyToJump = sm.AddAnyStateTransition(jumpState);
        anyToJump.hasExitTime = false;
        anyToJump.canTransitionToSelf = false;
        anyToJump.duration = 0.08f;
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

        var jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0.15f;
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

        // Attack 1: AnyState -> Attack1 (ComboStep == 1)
        var anyToA1 = sm.AddAnyStateTransition(a1State);
        anyToA1.hasExitTime = false;
        anyToA1.canTransitionToSelf = false;
        anyToA1.duration = 0.08f;
        anyToA1.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToA1.AddCondition(AnimatorConditionMode.Equals, 1, "ComboStep");

        // Attack1 -> Attack2 (Combo Window: ~0.50)
        var a1ToA2 = a1State.AddTransition(a2State);
        a1ToA2.hasExitTime = true;
        a1ToA2.exitTime = 0.50f;
        a1ToA2.duration = 0.12f;
        a1ToA2.hasFixedDuration = false;
        a1ToA2.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        a1ToA2.AddCondition(AnimatorConditionMode.Equals, 2, "ComboStep");

        // Attack1 -> Idle
        var a1ToIdle = a1State.AddTransition(idleState);
        a1ToIdle.hasExitTime = true;
        a1ToIdle.exitTime = 0.88f;
        a1ToIdle.duration = 0.15f;

        // Attack2 -> Attack3 (Combo Window: ~0.50)
        var a2ToA3 = a2State.AddTransition(a3State);
        a2ToA3.hasExitTime = true;
        a2ToA3.exitTime = 0.50f;
        a2ToA3.duration = 0.12f;
        a2ToA3.hasFixedDuration = false;
        a2ToA3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        a2ToA3.AddCondition(AnimatorConditionMode.Equals, 3, "ComboStep");

        // Attack2 -> Idle
        var a2ToIdle = a2State.AddTransition(idleState);
        a2ToIdle.hasExitTime = true;
        a2ToIdle.exitTime = 0.88f;
        a2ToIdle.duration = 0.15f;

        // Attack3 -> Idle
        var a3ToIdle = a3State.AddTransition(idleState);
        a3ToIdle.hasExitTime = true;
        a3ToIdle.exitTime = 0.90f;
        a3ToIdle.duration = 0.15f;

        // Aerial JumpAttack
        var anyToJA = sm.AddAnyStateTransition(jaState);
        anyToJA.hasExitTime = false;
        anyToJA.canTransitionToSelf = false;
        anyToJA.duration = 0.08f;
        anyToJA.AddCondition(AnimatorConditionMode.If, 0, "JumpAttack");

        var jaToIdle = jaState.AddTransition(idleState);
        jaToIdle.hasExitTime = true;
        jaToIdle.exitTime = 0.85f;
        jaToIdle.duration = 0.15f;
        jaToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

        AssetDatabase.SaveAssets();

        // Also update copy in Resources so runtime fallback is always in sync!
        AssetDatabase.CopyAsset(controllerPath, "Assets/Resources/MidasController.controller");
        AssetDatabase.SaveAssets();

        Debug.Log("<b>Animator Controller 3'LÜ KOMBO (Attack1 -> Attack2 -> Attack3) olarak başarıyla güncellendi!</b>");
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object obj in assets)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
