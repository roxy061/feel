"use client";

import { WorksWheel, type WorksWheelItem } from "@/components/ui/works-wheel";

// Curated high-resolution Unsplash stock imagery for reliable rendering
const WORKS: WorksWheelItem[] = [
  {
    title: "Prismatic Rift",
    image: "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=800&q=80",
    href: "#prismatic-rift",
  },
  {
    title: "Ember Clouds",
    image: "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=800&q=80",
    href: "#ember-clouds",
  },
  {
    title: "Neon Portal",
    image: "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?auto=format&fit=crop&w=800&q=80",
    href: "#neon-portal",
  },
  {
    title: "Red Ribbon",
    image: "https://images.unsplash.com/photo-1541701494587-cb58502866ab?auto=format&fit=crop&w=800&q=80",
    href: "#red-ribbon",
  },
  {
    title: "Celestial",
    image: "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?auto=format&fit=crop&w=800&q=80",
    href: "#celestial",
  },
  {
    title: "Uplight",
    image: "https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=800&q=80",
    href: "#uplight",
  },
  {
    title: "Indigo Marble",
    image: "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?auto=format&fit=crop&w=800&q=80",
    href: "#indigo-marble",
  },
  {
    title: "Launch Window",
    image: "https://images.unsplash.com/photo-1517976487515-59a9e69d7675?auto=format&fit=crop&w=800&q=80",
    href: "#launch-window",
  },
  {
    title: "Cosmic Wave",
    image: "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=800&q=80",
    href: "#cosmic-wave",
  },
];

export default function WorksWheelDemo() {
  return (
    <div className="bg-background text-foreground w-full h-screen">
      <WorksWheel items={WORKS} label="Works '26" action="View" />
    </div>
  );
}
