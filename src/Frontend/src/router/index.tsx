import { createBrowserRouter } from 'react-router-dom';
import { App } from '../App';
import { MatrixPage } from '../pages/Matrix/MatrixPage';
import { ListenersPage } from '../pages/Listeners/ListenersPage';
import { ReceiversPage } from '../pages/Receivers/ReceiversPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <MatrixPage /> },
      { path: 'listeners', element: <ListenersPage /> },
      { path: 'receivers', element: <ReceiversPage /> },
    ],
  },
]);
