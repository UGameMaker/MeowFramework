﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Panty
{
    public abstract class UnRegisterTrigger : MonoBehaviour
    {
        private Action mUnRegisterAction;
        public void Add(Action e) => mUnRegisterAction += e;
        protected void UnRegister() => mUnRegisterAction?.Invoke();
    }
    public class UnRegisterOnDestroyTrigger : UnRegisterTrigger
    {
        private void OnDestroy() => UnRegister();
    }
    public class UnRegisterOnDisableTrigger : UnRegisterTrigger
    {
        private void OnDisable() => UnRegister();
    }
    public class MonoKit : MonoSingle<MonoKit>
    {
        public static event Action OnUpdate;
        public static event Action OnFixedUpdate;
        public static event Action OnLateUpdate;
        public static event Action OnGuiUpdate;

        private void Update() => OnUpdate?.Invoke();
        private void FixedUpdate() => OnFixedUpdate?.Invoke();
        private void LateUpdate() => OnLateUpdate?.Invoke();
        private void OnGUI() => OnGuiUpdate?.Invoke();
    }
    public static partial class HubTool
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize() => MonoKit.GetIns();
        /// <summary>
        /// 尝试从一个物体身上获取脚本 如果获取不到就添加一个
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject o) where T : Component
        {
            T t = o.GetComponent<T>();
            if (t == null) t = o.AddComponent<T>();
            return t;
        }
        /// <summary>
        /// 找到面板父节点下所有对应控件
        /// </summary>
        public static void FindChildrenControl<T>(this Component mono, Action<string, T> callback = null) where T : Component
        {
#if UNITY_EDITOR
            if (callback == null) throw new Exception("无效回调");
#endif
            T[] controls = mono.GetComponentsInChildren<T>(true);
            if (controls.Length == 0) return;
            foreach (T ctrl in controls)
                callback.Invoke(ctrl.gameObject.name, ctrl);
        }
        public static T Error<T>(this T o)
        {
            Debug.unityLogger.Log(LogType.Error, o);
            return o;
        }
        public static T Warning<T>(this T o)
        {
            Debug.unityLogger.Log(LogType.Warning, o);
            return o;
        }
        public static void DrawBox(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, float duration = 0)
        {
            Debug.DrawLine(a, b, color, duration);
            Debug.DrawLine(b, c, color, duration);
            Debug.DrawLine(c, d, color, duration);
            Debug.DrawLine(d, a, color, duration);
        }
        public static void DrawBox(Vector2 origin, Vector2 size, Color color, float duration = 0)
        {
            Vector2 _half = size * 0.5f;

            float x1 = origin.x - _half.x;
            float x2 = origin.x + _half.x;
            float y1 = origin.y - _half.y;
            float y2 = origin.y + _half.y;

            var a = new Vector2(x1, y2);
            var b = new Vector2(x2, y2);
            var c = new Vector2(x2, y1);
            var d = new Vector2(x1, y1);

            DrawBox(a, b, c, d, color, duration);
        }
    }
    public static partial class HubEx
    {
        /// <summary>
        /// 获取系统层 Module 的别名
        /// </summary>
        public static S GetSystem<S>(this IPermissionProvider self) where S : class, IModule => self.Hub.Module<S>();
        /// <summary>
        /// 获取模型层 Module 的别名
        /// </summary>
        public static M GetModel<M>(this IPermissionProvider self) where M : class, IModule => self.Hub.Module<M>();
        /// <summary>
        /// 添加事件的监听 并标记为物体被销毁时注销
        /// </summary>
        public static void AddEvent_OnDestroyed_UnRegister<E>(this IPermissionProvider self, Action<E> evt, GameObject o) where E : struct
        {
            self.Hub.AddEvent<E>(evt);
            o.GetOrAddComponent<UnRegisterOnDestroyTrigger>().Add(() => self.Hub.RmvEvent<E>(evt));
        }
        /// <summary>
        /// 添加通知的监听 并标记为物体被销毁时注销
        /// </summary>
        public static void AddNotify_OnDestroyed_UnRegister<E>(this IPermissionProvider self, Action evt, GameObject o) where E : struct
        {
            self.Hub.AddNotify<E>(evt);
            o.GetOrAddComponent<UnRegisterOnDestroyTrigger>().Add(() => self.Hub.RmvNotify<E>(evt));
        }
        /// <summary>
        /// 添加事件的监听 并标记为物体失活时注销
        /// </summary>
        public static void AddEvent_OnDisabled_UnRegister<E>(this IPermissionProvider self, Action<E> evt, GameObject o) where E : struct
        {
            self.Hub.AddEvent<E>(evt);
            o.GetOrAddComponent<UnRegisterOnDisableTrigger>().Add(() => self.Hub.RmvEvent<E>(evt));
        }
        /// <summary>
        /// 添加通知的监听 并标记为物体失活时注销
        /// </summary>
        public static void AddNotify_OnDisabled_UnRegister<E>(this IPermissionProvider self, Action evt, GameObject o) where E : struct
        {
            self.Hub.AddNotify<E>(evt);
            o.GetOrAddComponent<UnRegisterOnDisableTrigger>().Add(() => self.Hub.RmvNotify<E>(evt));
        }
        /// <summary>
        /// 添加事件的监听 并标记为场景卸载时注销
        /// </summary>
        public static void AddEvent_OnSceneUnload_UnRegister<E>(this IPermissionProvider self, Action<E> evt) where E : struct
        {
            self.Hub.AddEvent<E>(evt);
            mWaitUninstEvents.Push(() => self.Hub.RmvEvent<E>(evt));
        }
        /// <summary>
        /// 添加通知的监听 并标记为场景卸载时注销
        /// </summary>
        public static void AddNotify_OnSceneUnload_UnRegister<N>(this IPermissionProvider self, Action evt) where N : struct
        {
            self.Hub.AddNotify<N>(evt);
            mWaitUninstEvents.Push(() => self.Hub.RmvNotify<N>(evt));
        }
        /// <summary>
        /// 用于当前场景卸载时 注销所有事件和通知
        /// </summary>
        public static void UnRegisterAllUnloadEvents()
        {
            while (mWaitUninstEvents.Count > 0)
                mWaitUninstEvents.Pop().Invoke();
        }
        // 用于存储所有当前场景卸载时 需要注销的事件和通知
        private static Stack<Action> mWaitUninstEvents = new Stack<Action>();
    }
    public abstract partial class ModuleHub<H>
    {
        protected ModuleHub()
        {
            Application.quitting += async () =>
            {
                await Task.Yield();
                this.Dispose();
            };
            // 预注册场景卸载事件
            SceneManager.sceneUnloaded +=
                scene => HubEx.UnRegisterAllUnloadEvents();
        }
    }
}