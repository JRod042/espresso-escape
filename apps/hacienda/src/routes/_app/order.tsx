import { createFileRoute, Outlet } from "@tanstack/react-router";

type Search = { cat?: string };

export const Route = createFileRoute("/_app/order")({
  validateSearch: (s: Record<string, unknown>): Search => ({
    cat: typeof s.cat === "string" ? s.cat : undefined,
  }),
  component: OrderLayout,
});

function OrderLayout() {
  return <Outlet />;
}
