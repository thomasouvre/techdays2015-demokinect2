using System;
using System.Collections.Generic;
using System.Text;
#if NETFX_CORE
using WindowsPreview.Kinect;
#else
using Microsoft.Kinect;
#endif

namespace DevAndSports.Kinect
{
    public class BonesConnection
    {
        public JointType Parent { get; set; }

        public IReadOnlyList<BonesConnection> Children { get; set; }

        public BonesConnection(JointType parent)
            : this(parent, new BonesConnection[0])
        {
        }

        public BonesConnection(JointType parent, IReadOnlyList<BonesConnection> children)
        {
            Parent = parent;
            Children = children;
        }

        public BonesConnection this[JointType key]
        {
            get
            {
                return Find(e => e.Parent == key);
            }
        }

        private BonesConnection Find(Predicate<BonesConnection> predicate)
        {
            if (predicate(this)) return this;
            foreach (var child in Children)
            {
                var res = child.Find(predicate);
                if (res != null) return res;
            }
            return null;
        }

        public IList<BonesConnection> GetFlatHierarchy(bool addSelf = true)
        {
            return GetFlatHierarchyOfT(e => e, addSelf);
        }

        public IList<JointType> GetFlatHierarchyOfJointTypes(bool addSelf = true)
        {
            return GetFlatHierarchyOfT(e => e.Parent, addSelf);
        }

        public IList<T> GetFlatHierarchyOfT<T>(Func<BonesConnection, T> selector, bool addSelf)
        {
            var flat = new List<T>();
            FlatPopulate(flat, selector, addSelf);
            return flat;
        }

        private void FlatPopulate<T>(IList<T> children, Func<BonesConnection, T> selector, bool addSelf)
        {
            if (addSelf)
                children.Add(selector(this));
            foreach (var child in Children)
                child.FlatPopulate(children, selector, true);
        }

        public static readonly BonesConnection SkeletalHierarchy = GetSkeletalHierarchy();

        private static BonesConnection GetSkeletalHierarchy()
        {
            return new BonesConnection(JointType.Head, new[]{
                new BonesConnection(JointType.Neck, new []{
                    new BonesConnection(JointType.SpineShoulder, new []{
                        new BonesConnection(JointType.SpineMid, new []{
                            new BonesConnection(JointType.SpineBase, new []{
                                new BonesConnection(JointType.HipRight,new []{
                                    new BonesConnection(JointType.KneeRight, new []{
                                        new BonesConnection(JointType.AnkleRight, new []{
                                            new BonesConnection(JointType.FootRight)})})}),
                                new BonesConnection(JointType.HipLeft,new []{
                                    new BonesConnection(JointType.KneeLeft, new []{
                                        new BonesConnection(JointType.AnkleLeft, new []{
                                            new BonesConnection(JointType.FootLeft)})})})})}),
                        new BonesConnection(JointType.ShoulderRight, new []{
                            new BonesConnection(JointType.ElbowRight, new []{
                                new BonesConnection(JointType.WristRight, new []{
                                    new BonesConnection(JointType.HandRight, new []{
                                        new BonesConnection(JointType.HandTipRight)}),
                                    new BonesConnection(JointType.ThumbRight)})})}),
                        new BonesConnection(JointType.ShoulderLeft, new []{
                            new BonesConnection(JointType.ElbowLeft, new []{
                                new BonesConnection(JointType.WristLeft, new []{
                                    new BonesConnection(JointType.HandLeft, new []{
                                        new BonesConnection(JointType.HandTipLeft)}),
                                    new BonesConnection(JointType.ThumbLeft)})})})})})});
        }
    }
}
