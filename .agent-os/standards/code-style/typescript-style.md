<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

## TypeScript Coding Style Guide

### General Best Practices

#### 1. **Code Structure \& Formatting**

- Use consistent indentation (2 or 4 spaces; prefer 2 for Vue projects).
- Limit line length to 100-120 characters for readability.
- Use semicolons to terminate statements for consistency.
- Prefer single quotes for strings (`'string'`) except for template literals.
- Name files in *kebab-case* (e.g., `user-profile.ts`).


#### 2. **Type Safety**

- Always annotate function parameters and return types.
- Prefer `interface` for object shapes and `type` for simple unions or primitives.
- Avoid `any` type; use `unknown` and type guards when unsure.
- Use type inference where appropriate, but do not sacrifice clarity for conciseness.


#### 3. **Variable Naming**

- Use *camelCase* for variables and functions.
- Use *PascalCase* for classes and types.
- Prefix interfaces with “I” is discouraged; rely on naming clarity.
- Use descriptive, context relevant names – avoid single-letter except for loop counters.


#### 4. **Functions \& Methods**

- Prefer arrow functions for inline and callback functions.
- Keep functions small and focused; one responsibility per function.
- Prefer pure functions and avoid shared mutable state.


#### 5. **Imports/Exports**

- Use ES6 module syntax: `import ... from ...` / `export ...`.
- Group imports by library origin: external > internal > CSS/assets.
- Avoid default exports unless warranted; named exports are more explicit.


#### 6. **Code Comments**

- Use JSDoc for public APIs; keep comments concise and relevant.
- Avoid redundant comments; code should be self-explanatory.
- Document all TODO/FIXME comments with actionable descriptions.


#### 7. **Error Handling**

- Use try/catch blocks for async operations.
- Fail fast: throw explicit errors with helpful messages.
- Validate external data (API responses, user input) regularly.


#### 8. **Testing**

- Write unit tests for key logic using Jest or your preferred framework.
- Prefer shallow testing for UI components, full coverage for core logic.
- Mock dependencies in tests when possible.

***

### Additional Best Practices for Vue.js with TypeScript

#### 1. **Component Structure**

- Use the composition API for new components (`setup()` over options API).
- Name components in PascalCase (e.g., `UserProfile.vue`).
- Keep one component per `.vue` file.
- Organize code into `script`, `template`, and `style` sections.


#### 2. **TypeScript in Vue Components**

- Always type props, events, slots, and emits.
- Use `defineProps<T>()`, `defineEmits<T>()` for prop and event typing.
- Avoid using refs with `any` types; specify generic types when using `ref<T>()`.
- Prefer reactive primitives (`reactive`, `ref`) over `data()`.


#### 3. **State Management**

- When using Vuex or Pinia, strongly type your store, getters, actions, and state.
- Use modules for scalable store architecture.
- Avoid mutating state outside of store actions.


#### 4. **Template Best Practices**

- Use `v-bind`/`:` and `v-on`/`@` consistently for props and events.
- Prefer explicit prop passing over `$attrs`.
- Avoid inline JavaScript expressions in templates; use computed properties.


#### 5. **Reactivity \& Lifecycle**

- Prefer `watchEffect` and `watch` over convoluted lifecycle hooks.
- Use lifecycle hooks (`onMounted`, `onUnmounted`) judiciously.
- Clean up side effects on component unmount.


#### 6. **Styling**

- Scope styles in components (`<style scoped>`).
- Use CSS modules or variables for consistent theming.


#### 7. **Linting \& Tooling**

- Use ESLint with TypeScript configurations, and integrate Prettier for auto-formatting.
- Configure lint rules specific to your project needs, focusing on type safety and component organization.
- Use Volar or Vetur for enhanced TypeScript support in Vue.


#### 8. **File Naming and Organization**

- Organize components by domain, not technical type.
- Use consistent folder naming (`components`, `stores`, `composables`, `assets`).

***

## Example

```typescript
// my-button.vue
<script lang="ts" setup>
import { defineProps, defineEmits } from 'vue'

interface ButtonProps {
  label: string;
  disabled?: boolean;
}

const props = defineProps<ButtonProps>()
const emit = defineEmits<{ (e: 'click'): void }>()

function handleClick() {
  if (!props.disabled) emit('click')
}
</script>

<template>
  <button :disabled="props.disabled" @click="handleClick">
    {{ props.label }}
  </button>
</template>

<style scoped>
button { padding: 8px 16px; }
</style>
```


***

Apply these practices consistently across your TypeScript and Vue projects to ensure readable, maintainable, and robust code.

