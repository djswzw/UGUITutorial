// 使用方法:
// 1. 将此脚本附加到你的内层ScrollRect GameObject上，它会自动取代原有的ScrollRect功能。
// 2. 确保你的外层ScrollRect也使用了此脚本，或是一个能接收事件的标准ScrollRect。
// 3. 根据需要，设置好内外层ScrollRect的Horizontal和Vertical属性（例如，外层只开启Vertical，内层只开启Horizontal）。

using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions // 建议将扩展组件放在独立的命名空间下
{
    /// <summary>
    /// 一个增强版的ScrollRect，用于解决嵌套ScrollRect的滚动冲突问题。
    /// 它通过在拖拽开始时判断用户的滑动意图，来决定是将事件路由给父级还是由自身处理。
    /// </summary>
    [AddComponentMenu("UI/Extensions/Nested Scroll Rect")]
    public class NestedScrollRect : ScrollRect
    {
        // 核心状态标记，用于决定当前拖拽事件流是否应路由给父级。
        // true表示当前拖拽应由父级处理。
        private bool m_RouteToParent = false;

        /// <summary>
        /// 向上遍历，为所有父级中实现了指定事件接口的组件，执行一个操作。
        /// </summary>
        /// <typeparam name="T">事件接口类型，必须继承自IEventSystemHandler</typeparam>
        /// <param name="action">要对父组件执行的操作</param>
        private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                // 使用GetComponents来查找所有可能接收事件的组件
                foreach (var component in parent.GetComponents<Component>())
                {
                    // 检查组件是否实现了目标接口
                    if (component is T)
                    {
                        action((T)(IEventSystemHandler)component);
                    }
                }
                parent = parent.parent;
            }
        }

        /// <summary>
        /// OnInitializePotentialDrag 通常应无条件上传，以确保所有父级都能正确初始化拖拽状态。
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // 将事件同时传递给父级和自身
            DoForParents<IInitializePotentialDragHandler>((parent) => { parent.OnInitializePotentialDrag(eventData); });
            base.OnInitializePotentialDrag(eventData);
        }

        /// <summary>
        /// 在拖拽开始时，进行核心的“意图判断”。
        /// </summary>
        public override void OnBeginDrag(PointerEventData eventData)
        {
            // 获取拖拽向量在水平和垂直方向上的绝对值
            float horizontalDelta = Math.Abs(eventData.delta.x);
            float verticalDelta = Math.Abs(eventData.delta.y);

            // 默认不路由给父级
            m_RouteToParent = false;

            if (horizontalDelta > verticalDelta)
            {
                // 用户的主要意图是“水平”滑动
                if (!horizontal)
                {
                    // 如果当前ScrollRect不支持水平滑动，则确定将事件路由给父级
                    m_RouteToParent = true;
                }
            }
            else // verticalDelta >= horizontalDelta
            {
                // 用户的主要意图是“垂直”滑动
                if (!vertical)
                {
                    // 如果当前ScrollRect不支持垂直滑动，则确定将事件路由给父级
                    m_RouteToParent = true;
                }
            }

            if (m_RouteToParent)
            {
                // 标记为路由后，将事件传递给父级，自身不处理
                DoForParents<IBeginDragHandler>((parent) => { parent.OnBeginDrag(eventData); });
            }
            else
            {
                // 标记为不路由，则调用基类（ScrollRect）的逻辑，自己处理
                base.OnBeginDrag(eventData);
            }
        }

        /// <summary>
        /// 在拖拽过程中，根据之前在OnBeginDrag中判断的状态，进行路由。
        /// </summary>
        public override void OnDrag(PointerEventData eventData)
        {
            if (m_RouteToParent)
            {
                DoForParents<IDragHandler>((parent) => { parent.OnDrag(eventData); });
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        /// <summary>
        /// 在拖拽结束时，进行路由，并重置状态。
        /// </summary>
        public override void OnEndDrag(PointerEventData eventData)
        {
            if (m_RouteToParent)
            {
                DoForParents<IEndDragHandler>((parent) => { parent.OnEndDrag(eventData); });
            }
            else
            {
                base.OnEndDrag(eventData);
            }

            // 无论如何，在拖拽结束后，必须重置路由标记，以备下次拖拽
            m_RouteToParent = false;
        }

        /// <summary>
        /// 对滚轮事件（OnScroll）的处理逻辑与拖拽类似，但使用scrollDelta。
        /// </summary>
        public override void OnScroll(PointerEventData eventData)
        {
            // 获取滚轮在水平和垂直方向上的滚动量绝对值
            float horizontalScroll = Math.Abs(eventData.scrollDelta.x);
            float verticalScroll = Math.Abs(eventData.scrollDelta.y);

            bool shouldRoute = false;
            if (horizontalScroll > verticalScroll)
            {
                // 滚轮意图是水平滚动
                if (!horizontal)
                    shouldRoute = true;
            }
            else // verticalScroll >= horizontalScroll
            {
                // 滚轮意图是垂直滚动
                if (!vertical)
                    shouldRoute = true;
            }

            if (shouldRoute)
            {
                DoForParents<IScrollHandler>((parent) => { parent.OnScroll(eventData); });
            }
            else
            {
                base.OnScroll(eventData);
            }
        }
    }
}