using UnityEngine;

namespace Bellator.AnimationPreview
{
    /// <summary>Isolated clip/controller viewer. Never attached to the gameplay player.</summary>
    public sealed class AnimationPreviewPlayer : MonoBehaviour
    {
        public AnimationPreviewCatalog catalog;
        public Camera previewCamera;
        GameObject character;
        Animator animator;
        Renderer[] visualRenderers;
        int classIndex = 2, clipIndex;
        float speed = 1, yaw = 20, scrub;
        bool paused, repeat = true, controllerMode, locked, running, blocking;
        float moveX, moveY, travelSpeed;
        Vector2 scroll;
        public Animator CurrentAnimator => animator;
        public int ClassIndex => classIndex;

        void Start() { SelectClass(classIndex); }
        void OnDestroy() { if (character != null) Destroy(character); }
        public void SelectClass(int index)
        {
            if (catalog == null || catalog.characters.Length == 0) return;
            classIndex = Mathf.Clamp(index, 0, catalog.characters.Length - 1);
            if (classIndex != 3 && clipIndex >= 13) clipIndex = 0;
            if (character != null) { character.SetActive(false); Destroy(character); }
            character = Instantiate(catalog.characters[classIndex].prefab, transform);
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;
            animator = character.GetComponentInChildren<Animator>();
            visualRenderers = character.GetComponentsInChildren<Renderer>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SelectClip(clipIndex);
        }
        public void SelectClip(int index)
        {
            clipIndex = Mathf.Clamp(index, 0, catalog.stateNames.Length - 1);
            controllerMode = false; paused = false; scrub = 0;
            animator.Rebind(); animator.Update(0);
            animator.Play(catalog.stateNames[clipIndex], 0, 0);
            animator.Update(0);
        }
        void Update()
        {
            if (animator == null) return;
            animator.speed = paused ? 0 : speed;
            if (controllerMode)
            {
                animator.SetFloat("Speed", travelSpeed); animator.SetFloat("MoveX", moveX); animator.SetFloat("MoveY", moveY);
                animator.SetBool("IsLockedOn", locked); animator.SetBool("IsRunning", running); animator.SetBool("IsBlocking", blocking);
            }
            else
            {
                // Direct inspection holds every selected state, including non-looping death.
                // Normal controller transitions are exercised separately in Controller mode.
                float duration = catalog.characters[classIndex].clips[clipIndex].length;
                float stateSpeed = catalog.characters[classIndex].playbackSpeed;
                scrub = Mathf.Min(1, scrub + (paused ? 0 : Time.deltaTime * speed * stateSpeed / duration));
                if (scrub >= 1 && repeat && clipIndex != 12) scrub = 0;
                animator.Play(catalog.stateNames[clipIndex], 0, Mathf.Min(scrub, .99999f));
                animator.Update(0);
            }
            if (previewCamera != null)
            {
                Vector3 target = transform.position + Vector3.up * (classIndex == 1 ? 1.4f : 1.15f);
                previewCamera.transform.position = target + Quaternion.Euler(-12, yaw, 0) * new Vector3(0, 0, classIndex == 1 ? 8.5f : 6.8f);
                previewCamera.transform.LookAt(target);
                var bounds = visualRenderers[0].bounds;
                foreach (var renderer in visualRenderers) bounds.Encapsulate(renderer.bounds);
                FrameBounds(previewCamera, bounds, target, classIndex == 1 ? 8.5f : 6.8f);
            }
        }
        public static void FrameBounds(Camera camera, Bounds bounds, Vector3 target, float minimumDistance)
        {
            float tangent = Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) * .86f;
            float distance = minimumDistance;
            var inverse = Quaternion.Inverse(camera.transform.rotation);
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                Vector3 local = inverse * (corner - target);
                distance = Mathf.Max(distance, Mathf.Abs(local.y) / tangent - local.z, Mathf.Abs(local.x) / (tangent * camera.aspect) - local.z);
            }
            camera.transform.position = target - camera.transform.forward * distance;
        }
        void OnGUI()
        {
            if (catalog == null || animator == null) return;
            float scale = Mathf.Clamp(Screen.height / 900f, .65f, 1.5f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUILayout.BeginArea(new Rect(18, 18, 310, Screen.height / scale - 36), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("BELLATOR / LABORATORIO DE ANIMACOES");
            GUILayout.Label("Etapa 1 · preview isolado · root motion OFF");
            for (int i = 0; i < catalog.characters.Length; i++)
                if (GUILayout.Toggle(classIndex == i, catalog.characters[i].label, GUI.skin.button) && classIndex != i) SelectClass(i);
            GUILayout.Space(8);
            for (int i = 0; i < (classIndex == 3 ? catalog.labels.Length : 13); i++)
                if (GUILayout.Button((!controllerMode && clipIndex == i ? "▶ " : "") + catalog.labels[i])) SelectClip(i);
            GUILayout.Space(8);
            paused = GUILayout.Toggle(paused, "Pausar"); repeat = GUILayout.Toggle(repeat, "Repetir (morte fica no final)");
            GUILayout.Label("Velocidade: " + speed.ToString("0.00") + "x"); speed = GUILayout.HorizontalSlider(speed, .15f, 1.75f);
            GUILayout.Label("Tempo do clip: " + scrub.ToString("P0"));
            float value = GUILayout.HorizontalSlider(scrub, 0, 1);
            if (!Mathf.Approximately(value, scrub)) { scrub = value; paused = true; animator.Play(catalog.stateNames[clipIndex], 0, Mathf.Min(scrub, .99999f)); animator.Update(0); }
            GUILayout.Label("Girar camera de teste"); yaw = GUILayout.HorizontalSlider(yaw, -180, 180);
            if (GUILayout.Button("Reiniciar clip")) SelectClip(clipIndex);
            GUILayout.Space(10);
            if (GUILayout.Button("Testar transicoes do Animator"))
            { animator.Rebind(); animator.Update(0); animator.Play("FreeLocomotion"); controllerMode = true; paused = false; }
            if (controllerMode)
            {
                locked = GUILayout.Toggle(locked, "IsLockedOn"); running = GUILayout.Toggle(running, "IsRunning"); blocking = GUILayout.Toggle(blocking, "IsBlocking");
                GUILayout.Label("Speed"); travelSpeed = GUILayout.HorizontalSlider(travelSpeed, 0, 1);
                GUILayout.Label("MoveX"); moveX = GUILayout.HorizontalSlider(moveX, -1, 1);
                GUILayout.Label("MoveY"); moveY = GUILayout.HorizontalSlider(moveY, -1, 1);
                foreach (string trigger in new[] { "Dodge", "LightAttack", "HeavyAttack", "Hit", "Fire", "Reload", "Die" })
                    if ((classIndex == 3 || (trigger != "Fire" && trigger != "Reload")) && GUILayout.Button(trigger)) animator.SetTrigger(trigger);
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
